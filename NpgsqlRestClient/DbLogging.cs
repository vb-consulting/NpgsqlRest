using System.Text.RegularExpressions;
using Npgsql;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace NpgsqlRestClient;

public class PostgresSink : ILogEventSink
{
    private readonly string _command;
    private readonly LogEventLevel _restrictedToMinimumLevel;
    private readonly int _paramCount;

    public PostgresSink(string command, LogEventLevel restrictedToMinimumLevel, int paramCount)
    {
        _command = command;
        _restrictedToMinimumLevel = restrictedToMinimumLevel;
        _paramCount = paramCount;
    }

    public void Emit(LogEvent logEvent)
    {
        if (string.IsNullOrEmpty(Builder.ConnectionString) is true)
        {
            return;
        }
        if (logEvent.Level < _restrictedToMinimumLevel)
        {
            return;
        }

        try
        {
            using var connection = new NpgsqlConnection(Builder.ConnectionString);
            using var command = new NpgsqlCommand(_command, connection);

            if (_paramCount > 0)
            {
                command.Parameters.Add(new NpgsqlParameter() { Value = logEvent.Level.ToString() }); // $1
            }
            if (_paramCount > 1)
            {
                command.Parameters.Add(new NpgsqlParameter() { Value = logEvent.RenderMessage() }); // $2
            }
            if (_paramCount > 2)
            {
                command.Parameters.Add(new NpgsqlParameter() { Value = logEvent.Timestamp.UtcDateTime }); // $3
            }
            if (_paramCount > 3)
            {
                command.Parameters.Add(new NpgsqlParameter() { Value = logEvent.Exception?.ToString() ?? (object)DBNull.Value }); // $4
            }
            if (_paramCount > 4)
            {
                command.Parameters.Add(new NpgsqlParameter() { Value = logEvent.Properties["SourceContext"]?.ToString()?.Trim('"') ?? (object)DBNull.Value }); // $5
            }
            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error writing to Postgres Log Sink:");
            Console.WriteLine(ex);
            Console.ResetColor();
        }
    }
}

public static partial class PostgresSinkSinkExtensions
{
    public static LoggerConfiguration Postgres(this LoggerSinkConfiguration loggerConfiguration, 
        string command,
        LogEventLevel restrictedToMinimumLevel)
    {
        var matches = ParameterRegex().Matches(command).ToArray();
        if (matches.Length < 1 || matches.Length > 5)
        {
            throw new ArgumentException("Command should have at least one parameter and maximum five parameters.");
        }
        for(int i = 0; i < matches.Length; i++)
        {
            if (matches[i].Value != $"${i + 1}")
            {
                throw new ArgumentException($"Parameter ${i + 1} is missing in the command.");
            }
        }
        return loggerConfiguration.Sink(new PostgresSink(command, restrictedToMinimumLevel, matches.Length));
    }

    [GeneratedRegex(@"\$\d+")]
    public static partial Regex ParameterRegex();
}
