using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using NpgsqlRestClient;

namespace BenchmarkTests;

public class FormatStringTests
{
    private readonly Dictionary<string, string> replacements = new Dictionary<string, string>
    {
        { "name", "John" },
        { "place", "NpgsqlRest" }
    };

    [Benchmark]
    public ReadOnlySpan<char> FormatStringMethod()
    {
        ReadOnlySpan<char> input = "Hello, {name}! Welcome to {place}.";
        return DefaultResponseParser.FormatString(input, replacements);
    }

    [Benchmark]
    public string RegexMethod()
    {
        ReadOnlySpan<char> input = "Hello, {name}! Welcome to {place}.";
        string pattern = @"\{(\w+)\}";
        return Regex.Replace(input.ToString(), pattern, match =>
        {
            string key = match.Groups[1].Value;
            return replacements.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    [Benchmark]
    public string RegexCodeGenMethod()
    {
        ReadOnlySpan<char> input = "Hello, {name}! Welcome to {place}.";
        return ReplaceRegex.Value().Replace(input.ToString(), match =>
        {
            string key = match.Groups[1].Value;
            return replacements.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}

public static partial class ReplaceRegex
{
    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    public static partial Regex Value();
}