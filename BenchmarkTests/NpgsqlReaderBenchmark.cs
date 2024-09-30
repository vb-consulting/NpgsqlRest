using System;
using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using Npgsql;

namespace BenchmarkTests;

[MemoryDiagnoser]
public class NpgsqlReaderBenchmark
{
    private NpgsqlConnection _connection;
    private const string ConnectionString = "Host=localhost;Port=5436;Username=postgres;Password=postgres;Database=example";
    private const string Query = @"
            SELECT 
                generate_series(1, 5000) AS int_col,
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

    [Benchmark]
    public void GetValue()
    {
        using (var cmd = new NpgsqlCommand(Query, _connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                }
            }
        }
    }

    [Benchmark]
    public void GetProviderSpecificValue()
    {
        using (var cmd = new NpgsqlCommand(Query, _connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetProviderSpecificValue(i);
                }
            }
        }
    }
}