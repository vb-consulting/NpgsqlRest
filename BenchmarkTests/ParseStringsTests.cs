using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using NpgsqlRestClient;

namespace BenchmarkTests;

public class ParseStringsTests
{
    string[] names = { "test.txt", "x", "verylongfilename.txt", "abcde" };
    string[] patterns = { "*.txt", "*?*", "*long*.txt", "a?c*" };


    [Benchmark]
    public void IsPatternMatchMethod()
    {
        foreach (var name in names)
            foreach (var pattern in patterns)
            {
                DefaultResponseParser.IsPatternMatch(name, pattern);
            }
    }

    //[Benchmark]
    //public void LikePatternIsMethod()
    //{
    //    foreach (var name in names)
    //        foreach (var pattern in patterns)
    //        {
    //            DefaultResponseParser.LikePatternIsMatch(name, pattern);
    //        }
    //}

    //[Benchmark]
    //public void LikePatternIsMatchFastMethod()
    //{
    //    foreach (var name in names)
    //        foreach (var pattern in patterns)
    //        {
    //            DefaultResponseParser.LikePatternIsMatchFast(name, pattern);
    //        }
    //}
}
