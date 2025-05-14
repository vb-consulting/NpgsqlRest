using System;
using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using Npgsql;
using NpgsqlRest;

namespace BenchmarkTests;

public class NpgsqlCachedCommand : NpgsqlCommand
{
    private static readonly NpgsqlCachedCommand _instanceCache = new();

    private NpgsqlCommand CachedCommandClone() => MemberwiseClone() as NpgsqlCommand;

    public static NpgsqlCommand Create(NpgsqlConnection connection)
    {
        var result = _instanceCache.CachedCommandClone();
        result.Connection = connection;
        return result;
    }
}

public class NpgsqlCachedParameter : NpgsqlParameter
{
    private static readonly NpgsqlCachedParameter _textParam = new()
    {
        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
        Value = DBNull.Value,
    };

    public NpgsqlParameter CachedParameterMemberwiseClone() => MemberwiseClone() as NpgsqlParameter;

    public static NpgsqlParameter CreateTextParam(string? value)
    {
        var result = _textParam.CachedParameterMemberwiseClone();
        if (value is not null)
        {
            result.Value = value;
        }
        return result;
    }
}

[MemoryDiagnoser]
public class CacheCommandTests
{
    private string _connectionStr = "Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=postgres";
    
    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark(Baseline = true)]
    public async Task NormalCommand()
    {
        using var connection = new NpgsqlConnection(_connectionStr);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "select $1,$2,$3";
        cmd.Parameters.Add(new NpgsqlParameter
        { 
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = "test1"
        });
        cmd.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = "test2"
        });
        cmd.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = "test3"
        });
        await using var reader = await cmd.ExecuteReaderAsync();
    }

    [Benchmark]
    public async Task CachedCommand()
    {
        using var connection = new NpgsqlConnection(_connectionStr);
        connection.Open();
        using var cmd = NpgsqlCachedCommand.Create(connection);
        cmd.CommandText = "select $1,$2,$3";
        cmd.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = "test1"
        });
        cmd.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = "test2"
        });
        cmd.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text,
            Value = "test3"
        });
        await using var reader = await cmd.ExecuteReaderAsync();
    }

    [Benchmark]
    public async Task CachedCommandAndParemeters()
    {
        using var connection = new NpgsqlConnection(_connectionStr);
        connection.Open();
        using var cmd = NpgsqlCachedCommand.Create(connection);
        cmd.CommandText = "select $1,$2,$3";
        cmd.Parameters.Add(NpgsqlCachedParameter.CreateTextParam("test1"));
        cmd.Parameters.Add(NpgsqlCachedParameter.CreateTextParam("test2"));
        cmd.Parameters.Add(NpgsqlCachedParameter.CreateTextParam("test3"));
        await using var reader = await cmd.ExecuteReaderAsync();
    }
}
