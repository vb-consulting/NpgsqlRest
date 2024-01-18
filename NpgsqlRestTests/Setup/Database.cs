using System.Reflection;
using Npgsql;

namespace NpgsqlRestTests;

public static partial class Database
{
    private const string dbname = "npgsql_rest_test";
    private const string initialConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
    private static readonly StringBuilder script = new();

    static Database()
    {
        foreach (var method in typeof(Database).GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (method.GetParameters().Length == 0 && !string.Equals(method.Name, "Create", StringComparison.OrdinalIgnoreCase))
            {
                method.Invoke(null, []);
            }
        }
    }

    public static string Create()
    {
        DropIfExists();
        var builder = new NpgsqlConnectionStringBuilder(initialConnectionString)
        {
            Database = dbname
        };

        using NpgsqlConnection test = new(builder.ConnectionString);
        test.Open();
        using var command = test.CreateCommand();
        command.CommandText = script.ToString();
        command.ExecuteNonQuery();

        return builder.ConnectionString;
    }

    public static void DropIfExists()
    {
        using NpgsqlConnection connection = new(initialConnectionString);
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
        exec($"create database {dbname}");
        exec($"alter database {dbname} set timezone to 'UTC'");
    }
}
