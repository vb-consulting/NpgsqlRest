using System;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace BenchmarkTests;

public class HttpClientTests
{
    private HttpClient _client = null!;

    [GlobalSetup]
    public void Setup()
    {
        _client = new();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.Dispose();
    }

    [Benchmark]
    public async Task CallPerfTests()
    {
        string json = """
        {
            "records": 25,
            "textParam": "XYZ",
            "intParam": 3,
            "tsParam": "2024-04-04T03:03:03.00",
            "boolParam": false
        }
        """;
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var result = await _client.PostAsync("http://localhost:5000/api/perf-test/", content);
        var response = await result.Content.ReadAsStringAsync();
    }
}