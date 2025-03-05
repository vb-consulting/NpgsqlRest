using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using NpgsqlRest;

namespace NpgsqlRestClient;

public class DefaultResponseParser(
    string? userIdParameterName,
    string? userNameParameterName,
    string? userRolesParameterName,
    string? ipAddressParameterName,
    Dictionary<string, StringValues>? customClaims,
    Dictionary<string, string?>? customParameters) : IResponseParser
{
    private readonly string? userIdParameterName = userIdParameterName;
    private readonly string? userNameParameterName = userNameParameterName;
    private readonly string? userRolesParameterName = userRolesParameterName;
    private readonly string? ipAddressParameterName = ipAddressParameterName;
    private readonly Dictionary<string, StringValues>? customClaims = customClaims;
    private readonly Dictionary<string, string?>? customParameters = customParameters;

    public ReadOnlySpan<char> Parse(ReadOnlySpan<char> input, RoutineEndpoint endpoint, HttpContext context)
    {
        Dictionary<string, string> replacements = [];
        context.User.FindFirst("claimType");
        if (userIdParameterName is not null)
        {
            var value = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            replacements.Add(userIdParameterName, value is null ? "NULL" : string.Concat('"', value, '"')); 
        }
        if (userNameParameterName is not null)
        {
            var value = context.User.Identity?.Name;
            replacements.Add(userNameParameterName, value is null ? "NULL" : string.Concat('"', value, '"'));
        }
        if (userRolesParameterName is not null)
        {
            var value = context.User.FindAll(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal))?.Select(r => string.Concat('"', r.Value, '"'));
            replacements.Add(userRolesParameterName, value is null ? "NULL" : string.Concat('[', string.Join(',', value), ']'));
        }
        if (ipAddressParameterName is not null)
        {
            var value = App.GetClientIpAddress(context.Request);
            replacements.Add(ipAddressParameterName, value is null ? "NULL" : string.Concat('"', value, '"'));
        }
        if (customClaims is not null)
        {
            foreach (var (key, value) in customClaims)
            {
                var claim = context.User.FindFirst(key);
                replacements.Add(key, claim is null ? "NULL" : string.Concat('"', claim.Value, '"'));
            }
        }
        if (customParameters is not null)
        {
            foreach (var (key, value) in customParameters)
            {
                replacements.Add(key, value is null ? "NULL" : string.Concat('"', value, '"'));
            }
        }
        return FormatString(input, replacements);
    }

    public static ReadOnlySpan<char> FormatString(ReadOnlySpan<char> input, Dictionary<string, string> replacements)
    {
        if (replacements is null || replacements.Count == 0)
        {
            return input;
        }

        int resultLength = 0;
        int currentPos = 0;
        int inputLength = input.Length;

        while (currentPos < inputLength)
        {
            int placeholderInfo = FindNextPlaceholder(input, currentPos, out int placeholderStart, out int placeholderEnd);

            if (placeholderInfo == 0)
            {
                resultLength += inputLength - currentPos;
                break;
            }
            else if (placeholderInfo == -1)
            {
                resultLength += inputLength - currentPos;
                break;
            }

            resultLength += placeholderStart - currentPos;

            ReadOnlySpan<char> keySpan = input[(placeholderStart + 1)..placeholderEnd];

            bool keyFound = false;
            string? value = null;

            foreach (var pair in replacements)
            {
                if (keySpan.SequenceEqual(pair.Key.AsSpan()))
                {
                    keyFound = true;
                    value = pair.Value;
                    break;
                }
            }

            if (keyFound && value != null)
            {
                resultLength += value.Length;
            }
            else
            {
                resultLength += placeholderEnd - placeholderStart + 1;
            }

            currentPos = placeholderEnd + 1;
        }

        char[] resultArray = new char[resultLength];
        Span<char> result = resultArray;

        currentPos = 0;
        int resultPos = 0;

        while (currentPos < inputLength)
        {
            int placeholderInfo = FindNextPlaceholder(input, currentPos, out int placeholderStart, out int placeholderEnd);

            if (placeholderInfo == 0)
            {
                var remainingChars = input[currentPos..];
                remainingChars.CopyTo(result[resultPos..]);
                resultPos += remainingChars.Length;
                break;
            }
            else if (placeholderInfo == -1)
            {
                var remainingChars = input[currentPos..];
                remainingChars.CopyTo(result[resultPos..]);
                resultPos += remainingChars.Length;
                break;
            }

            if (placeholderStart > currentPos)
            {
                var charsToCopy = input[currentPos..placeholderStart];
                charsToCopy.CopyTo(result[resultPos..]);
                resultPos += charsToCopy.Length;
            }

            ReadOnlySpan<char> keySpan = input[(placeholderStart + 1)..placeholderEnd];

            bool keyFound = false;
            string? value = null;

            foreach (var pair in replacements)
            {
                if (keySpan.SequenceEqual(pair.Key.AsSpan()))
                {
                    keyFound = true;
                    value = pair.Value;
                    break;
                }
            }

            if (keyFound && value != null)
            {
                value.AsSpan().CopyTo(result[resultPos..]);
                resultPos += value.Length;
            }
            else
            {
                var placeholderChars = input[placeholderStart..(placeholderEnd + 1)];
                placeholderChars.CopyTo(result[resultPos..]);
                resultPos += placeholderChars.Length;
            }

            currentPos = placeholderEnd + 1;
        }

        return resultArray;
    }

    private static int FindNextPlaceholder(ReadOnlySpan<char> input, int startPos, out int placeholderStart, out int placeholderEnd)
    {
        placeholderStart = -1;
        placeholderEnd = -1;

        int remainingLength = input.Length - startPos;
        if (remainingLength <= 0) 
        { 
            return 0;
        }

        ReadOnlySpan<char> remaining = input[startPos..];
        int openBracePos = remaining.IndexOf('{');

        if (openBracePos == -1)
        {
            return 0;
        }
        placeholderStart = startPos + openBracePos;

        ReadOnlySpan<char> afterOpenBrace = input[(placeholderStart + 1)..];
        int closeBracePos = afterOpenBrace.IndexOf('}');

        if (closeBracePos == -1) 
        {
            return -1;
        }

        placeholderEnd = placeholderStart + 1 + closeBracePos;
        return 1;
    }
}