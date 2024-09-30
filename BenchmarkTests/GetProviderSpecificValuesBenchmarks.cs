using System;
using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Npgsql;

namespace BenchmarkTests;

[MemoryDiagnoser]
public class GetProviderSpecificValuesBenchmarks
{
    private NpgsqlConnection _connection;
    private const string ConnectionString = "Host=localhost;Port=5436;Username=postgres;Password=postgres;Database=example";
    private const string Query = @"
            SELECT 
                generate_series(1, 1000) AS int_col,
                random() AS float_col,
                md5(random()::text) AS text_col,
                now() + (random() * interval '1 year') AS timestamp_col,
                ARRAY[random(), random(), random()] AS array_col,
                (random() > 0.5) AS bool_col,
                uuid_generate_v4() AS uuid_col,
                jsonb_build_object('key', md5(random()::text)) AS jsonb_col,
                point(random() * 100, random() * 100) AS point_col,
                inet(((random()*255)::int)::text || '.' || ((random()*255)::int)::text || '.' || ((random()*255)::int)::text || '.' || ((random()*255)::int)::text) AS inet_col
        ";

    [GlobalSetup]
    public void Setup()
    {
        _connection = new NpgsqlConnection(ConnectionString);
        _connection.Open();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void OriginalMethod_single_value_ordinal()
    {
        using var command = new NpgsqlCommand(Query, _connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                object value = reader.GetProviderSpecificValue(i);
            }
        }
    }

    [Benchmark]
    public void OptimizedMethod_multiple_values()
    {
        using var command = new NpgsqlCommand(Query, _connection);
        using var reader = command.ExecuteReader();

        var values = new object[reader.FieldCount];
        while (reader.Read())
        {
            reader.GetProviderSpecificValues(values);
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
            }
        }
    }

    [Benchmark]
    public unsafe void OptimizedMethod_multiple_values_unsafe()
    {
        using var command = new NpgsqlCommand(Query, _connection);
        using var reader = command.ExecuteReader();

        var values = new object[reader.FieldCount];
        while (reader.Read())
        {
            reader.GetProviderSpecificValues(values);
            fixed (object* pValues = values)
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = pValues[i];
                }
            }
        }
    }
}