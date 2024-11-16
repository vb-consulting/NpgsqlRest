using System;
using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Npgsql;

namespace BenchmarkTests;

[MemoryDiagnoser]
public class QuoteTextTests
{
    public static string QuoteText(ref ReadOnlySpan<char> value)
    {
        int newLength = value.Length + 2;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\"')
            {
                newLength++;
            }
        }
        Span<char> result = stackalloc char[newLength];
        result[0] = '"';
        int currentPos = 1;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\"')
            {
                result[currentPos++] = '\"';
                result[currentPos++] = '\"';
            }
            else
            {
                result[currentPos++] = value[i];
            }
        }
        result[currentPos] = '"';
        return new string(result);
    }
    
    [Benchmark(Baseline = true)]
    public void OriginalMethod()
    {
        StringBuilder sb = new();
        string raw = "This is a test string with a \"quote\" in it.";
        sb.Append('\"');
        sb.Append(raw.Replace("\"", "\"\""));
        sb.Append('\"');
    }
    
    [Benchmark]
    public void QuoteTextMethod()
    {
        StringBuilder sb = new();
        ReadOnlySpan<char> raw = "This is a test string with a \"quote\" in it.";
        sb.Append(QuoteText(ref raw));
    }
}