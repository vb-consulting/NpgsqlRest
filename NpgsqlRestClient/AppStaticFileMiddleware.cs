﻿using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Npgsql;

namespace NpgsqlRestClient;

public class AppStaticFileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _hostingEnv;
    private static readonly FileExtensionContentTypeProvider _fileTypeProvider = new();

    private static string[]? _parsePatterns = default!;
    private static Serilog.ILogger? _logger = default!;

    private static readonly ConcurrentDictionary<string, bool> _pathInParsePattern = new();
    // New cache for parsed file contents
    private static readonly ConcurrentDictionary<string, (byte[] Content, DateTimeOffset LastModified)> _parsedFileCache = new();
    private static readonly bool _cacheParsedFiles = true;
    private static IAntiforgery? _antiforgery;
    private static DefaultResponseParser? _parser;

    public static void ConfigureStaticFileMiddleware(
        bool parse,
        string[]? parsePatterns,
        string? userIdTag,
        string? userNameTag,
        string? userRolesTag,
        Dictionary<string, StringValues>? customClaimTags,
        bool cacheParsedFiles,
        string? antiforgeryFieldNameTag,
        string? antiforgeryTokenTag,
        IAntiforgery? antiforgery,
        Serilog.ILogger? logger)
    {
        _parsePatterns = parse == false || parsePatterns == null || parsePatterns.Length == 0 ? null : parsePatterns?.Where(p => !string.IsNullOrEmpty(p)).ToArray();
        if (parse is false || _parsePatterns is null ||
            (userIdTag is null && userNameTag is null && userRolesTag is null && customClaimTags is null && antiforgeryFieldNameTag is null && antiforgeryTokenTag is null))
        {
            _parser = null;
        }
        else
        {
            _parser = new DefaultResponseParser(
                    userIdParameterName: userIdTag,
                    userNameParameterName: userNameTag,
                    userRolesParameterName: userRolesTag,
                    ipAddressParameterName: null,
                    antiforgeryFieldNameTag: antiforgeryFieldNameTag,
                    antiforgeryTokenTag: antiforgeryTokenTag,
                    customClaims: customClaimTags,
                    customParameters: null);
        }

        _antiforgery = antiforgery;
        _logger = logger;
    }

    public AppStaticFileMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _hostingEnv = hostingEnv ?? throw new ArgumentNullException(nameof(hostingEnv));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string method = context.Request.Method;
        bool isGet = HttpMethods.IsGet(method);
        if (!isGet && !HttpMethods.IsHead(method))
        {
            await _next(context);
            return;
        }
        PathString path = context.Request.Path; // Cache PathString
        IFileInfo fileInfo = _hostingEnv.WebRootFileProvider.GetFileInfo(path);
        if (!fileInfo.Exists || fileInfo.IsDirectory)
        {
            await _next(context);
            return;
        }
        AntiforgeryTokenSet? tokenSet = null;
        if (_antiforgery is not null)
        {
            var pathStr = path.ToString();
            if (pathStr.EndsWith(".html") is true || pathStr.EndsWith(".htm") is true)
            {
                tokenSet = _antiforgery.GetAndStoreTokens(context);
            }
        }
        
        string contentType = fileInfo.PhysicalPath != null && _fileTypeProvider.TryGetContentType(fileInfo.PhysicalPath, out var ct)
            ? ct
            : "application/octet-stream";

        DateTimeOffset lastModified = fileInfo.LastModified.ToUniversalTime();
        long length = fileInfo.Length;
        long etagHash = lastModified.ToFileTime() ^ length;
        string etagString = string.Concat("\"", Convert.ToString(etagHash, 16), "\"");

        var ifNoneMatch = context.Request.Headers[HeaderNames.IfNoneMatch];
        if (!string.IsNullOrEmpty(ifNoneMatch) &&
            System.Net.Http.Headers.EntityTagHeaderValue.TryParse(ifNoneMatch, out var clientEtag) &&
            string.Equals(etagString, clientEtag.ToString(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        var ifModifiedSince = context.Request.Headers[HeaderNames.IfModifiedSince];
        if (!string.IsNullOrEmpty(ifModifiedSince) && DateTimeOffset.TryParse(ifModifiedSince, out var since) && since >= lastModified)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.Headers[HeaderNames.LastModified] = lastModified.ToString("R");
        context.Response.Headers[HeaderNames.ETag] = etagString;
        context.Response.Headers[HeaderNames.AcceptRanges] = "bytes";

        if (isGet)
        {
            try
            {
                byte[] buffer;
                if (_parser is null)
                {
                    context.Response.ContentLength = length;
                    using var fileStream = new FileStream(fileInfo.PhysicalPath!, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                    await fileStream.CopyToAsync(context.Response.Body, context.RequestAborted);
                    return;
                }

                var pathString = path.ToString();
                if (_pathInParsePattern.TryGetValue(pathString, out bool isInParsePattern) is false)
                {
                    isInParsePattern = false;
                    for (int i = 0; i < _parsePatterns?.Length; i++)
                    {
                        if (DefaultResponseParser.IsPatternMatch(pathString, _parsePatterns[i]))
                        {
                            isInParsePattern = true;
                            break;
                        }
                    }
                    _pathInParsePattern.TryAdd(pathString, isInParsePattern);
                }

                if (isInParsePattern is false)
                {
                    context.Response.ContentLength = length;
                    using var fileStream = new FileStream(fileInfo.PhysicalPath!, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                    await fileStream.CopyToAsync(context.Response.Body, context.RequestAborted);
                    return;
                }

                // Check cache for parsed files
                if (_cacheParsedFiles && _parsedFileCache.TryGetValue(pathString, out var cached) && cached.LastModified >= lastModified)
                {
                    buffer = cached.Content;
                }
                else
                {
                    // Read file from disk
                    using var fileStream = new FileStream(fileInfo.PhysicalPath!, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                    buffer = new byte[(int)fileInfo.Length];
                    await fileStream.ReadExactlyAsync(buffer, context.RequestAborted);

                    // Cache the file content if enabled
                    if (_cacheParsedFiles)
                    {
                        _parsedFileCache[pathString] = (buffer, lastModified);
                    }
                }

                int charCount = Encoding.UTF8.GetCharCount(buffer);
                char[] chars = ArrayPool<char>.Shared.Rent(charCount);
                try
                {
                    Encoding.UTF8.GetChars(buffer, 0, buffer.Length, chars, 0);
                    ReadOnlySpan<char> result = _parser.Parse(new ReadOnlySpan<char>(chars, 0, charCount), context, tokenSet);
                    var writer = PipeWriter.Create(context.Response.Body);
                    try
                    {
                        int maxBytesNeeded = Encoding.UTF8.GetMaxByteCount(result.Length);
                        Memory<byte> memory = writer.GetMemory(maxBytesNeeded);
                        int actualBytesWritten = Encoding.UTF8.GetBytes(result, memory.Span);
                        writer.Advance(actualBytesWritten);
                        context.Response.ContentLength = actualBytesWritten;
                        await writer.FlushAsync(context.RequestAborted);
                    }
                    finally
                    {
                        await writer.CompleteAsync();
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(chars);
                }
            }
            catch (IOException ex)
            {
                _logger?.Error(ex, "Failed to serve static file {Path}", path.ToString());

                context.Response.Clear();
                await _next(context);
                return;
            }
        }

        // HEAD request completes here
    }
}