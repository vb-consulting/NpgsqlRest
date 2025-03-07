using System;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization.Infrastructure;
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

        int inputLength = input.Length;

        if (inputLength == 0)
        {
            return input;
        }

        var lookup = replacements.GetAlternateLookup<ReadOnlySpan<char>>();

        int resultLength = 0;
        bool inside = false;
        int startIndex = 0;
        for (int i = 0; i < inputLength; i++)
        {
            var ch = input[i];
            if (ch == '{')
            {
                if (inside is true)
                {
                    resultLength += input[startIndex..i].Length;
                }
                inside = true;
                startIndex = i;
                continue;
            }

            if (ch == '}' && inside is true)
            {
                inside = false;
                if (lookup.TryGetValue(input[(startIndex + 1)..i], out var value))
                {
                    resultLength += value.Length;
                }
                else
                {
                    resultLength += input[startIndex..i].Length + 1;
                }
                continue;
            }

            if (inside is false)
            {
                resultLength += 1;
            }
        }
        if (inside is true)
        {
            resultLength += input[startIndex..].Length;
        }

        char[] resultArray = new char[resultLength];
        Span<char> result = resultArray;
        int resultPos = 0;

        inside = false;
        startIndex = 0;
        for(int i = 0; i < inputLength; i++)
        {
            var ch = input[i];
            if (ch == '{')
            {
                if (inside is true)
                {
                    input[startIndex..i].CopyTo(result[resultPos..]);
                    resultPos += input[startIndex..i].Length;
                }
                inside = true;
                startIndex = i;
                continue;
            }

            if (ch == '}' && inside is true)
            {
                inside = false;
                if (lookup.TryGetValue(input[(startIndex + 1)..i], out var value))
                {
                    value.AsSpan().CopyTo(result[resultPos..]);
                    resultPos += value.Length;
                }
                else
                {
                    input[startIndex..i].CopyTo(result[resultPos..]);
                    resultPos += input[startIndex..i].Length;
                    result[resultPos] = ch;
                    resultPos++;
                }
                continue;
            }

            if (inside is false)
            {
                result[resultPos] = ch;
                resultPos++;
            }
        }
        if (inside is true)
        {
            input[startIndex..].CopyTo(result[resultPos..]);
        }

        return resultArray;
    }
}