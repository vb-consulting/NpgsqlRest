using System.Reflection;
using Npgsql;

namespace NpgsqlRestTests;

public static partial class Database
{
    private const string initialName = "npgsql_rest_test";
    private const string initialConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
    private static string? dbname;
    private static readonly StringBuilder script = new();

    static Database()
    {
        foreach (var method in typeof(Database).GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (!method.GetParameters().Any())
            {
                method.Invoke(null, []);
            }
        }
    }

    public static string Create(bool addNamePrefix = false, bool recreate = true)
    {
        dbname = addNamePrefix ? string.Concat(initialName, "_", Guid.NewGuid().ToString()[..8]) : initialName;

        if (recreate)
        {
            DropIfExists(create: true);
        }
        
        var builder = new NpgsqlConnectionStringBuilder(initialConnectionString)
        {
            Database = dbname
        };

        using NpgsqlConnection test = new NpgsqlConnection(builder.ConnectionString);
        test.Open();
        using var command = test.CreateCommand();
        command.CommandText = script.ToString();
        command.ExecuteNonQuery();

        return builder.ConnectionString;
    }

    public static void DropIfExists(bool create)
    {
        if (string.IsNullOrEmpty(dbname))
        {
            return;
        }
        using NpgsqlConnection connection = new NpgsqlConnection(initialConnectionString);
        connection.Open();
        void exec(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        bool any(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return true;
            }
            return false;
        }

        if (any($"select 1 from pg_database where datname = '{dbname}'"))
        {
            exec($"revoke connect on database {dbname} from public");
            exec($"select pg_terminate_backend(pid) from pg_stat_activity where datname = '{dbname}' and pid <> pg_backend_pid()");
            exec($"drop database {dbname}");
        }

        if (create)
        {
            exec($"create database {dbname}");
        }
    }
}
