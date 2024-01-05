using Npgsql;

namespace NpgsqlRestTestWebApi;

public static class Database
{
    const string dbname = "npgsql_rest_test";
    
    public static string Init(string connectionString)
    {
        using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
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

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.Database = dbname;
        connectionString = builder.ConnectionString;

        using NpgsqlConnection test = new NpgsqlConnection(connectionString);
        test.Open();
        using var command = test.CreateCommand();
        command.CommandText = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "testcases.sql"));
        command.ExecuteNonQuery();

        return connectionString;
    }
}
