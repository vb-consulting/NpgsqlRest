using System.Text;
using Npgsql;
using NpgsqlRest;
using NpgsqlRest.HttpFiles;
using NpgsqlRest.TsClient;
using static NpgsqlRestClient.Config;

namespace NpgsqlRestClient;

public static class Arguments
{
    public static bool Parse(string[] args)
    {
        if (args.Any(a => a.ToLowerInvariant() is "-v" or "--version" or "-h" or "--help") is false)
        {
            return true;
        }

        if (args.Any(a => a.ToLowerInvariant() is "-h" or "--help") is true)
        {
            Line("Usage:");
            Line([
                ("npgsqlrest", "Run with the default configuration files: appsettings.json (required) and appsettings.Development.json (optional)."),
                ("npgsqlrest [files...]", "Run with the custom configuration files. All configuration files are required."),
                ("npgsqlrest [file1 -o file2...]", "Use the -o switch to mark the next configuration file as optional. The first file after the -o switch is optional."),
                ("npgsqlrest [file1 --optional file2...]", "Use --optional switch to mark the next configuration file as optional. The first file after the --optional switch is optional."),
                ("Note:", "Values in the later file will override the values in the previous one."),
                ("npgsqlrest [--key=value]", "Override the configuration with this key with a new value (case insensitive, use : to separate sections). "),
                (" ", " "),
                ("npgsqlrest -v, --version", "Show version information."),
                ("npgsqlrest -h, --help", "Show command line help."),
                (" ", " "),
                (" ", " "),
                ("Examples:", " "),
                ("Example: use two config files", "npgsqlrest appsettings.json appsettings.Development.json"),
                ("Example: second config file optional", "npgsqlrest appsettings.json -o appsettings.Development.json"),
                ("Example: override ApplicationName config", "npgsqlrest --applicationname=Test"),
                ("Example: override Auth:CookieName config", "npgsqlrest --auth:cookiename=Test"),
                ]);
        }

        if (args.Any(a => a.ToLowerInvariant() is "-v" or "--version") is true)
        {
            Line("Versions:");
            Line([
                ("Client Build", System.Reflection.Assembly.GetAssembly(typeof(Program))?.GetName()?.Version?.ToString() ?? "-"),
                ("Npgsql", System.Reflection.Assembly.GetAssembly(typeof(NpgsqlConnection))?.GetName()?.Version?.ToString() ?? "-"),
                ("NpgsqlRest", System.Reflection.Assembly.GetAssembly(typeof(NpgsqlRestOptions))?.GetName()?.Version?.ToString() ?? "-"),
                ("NpgsqlRest.HttpFiles", System.Reflection.Assembly.GetAssembly(typeof(HttpFileOptions))?.GetName()?.Version?.ToString() ?? "-"),
                ("NpgsqlRest.TsClient", System.Reflection.Assembly.GetAssembly(typeof(TsClientOptions))?.GetName()?.Version?.ToString() ?? "-"),
                (" ", " "),
                ("CurrentDirectory", CurrentDir)
                ]);
            NL();
        }
        return false;
    }

    public static (List<(string fileName, bool optional)> configFiles, string[] commanLineArgs) BuildFromArgs(string[] args)
    {
        var configFiles = new List<(string fileName, bool optional)>();
        var commandLineArgs = new List<string>();

        bool nextIsOptional = false;
        foreach (var arg in args)
        {
           
            if (arg.StartsWith('-'))
            {
                var lower = arg.ToLowerInvariant();
                if (lower is "-o" or "--optional")
                {
                    nextIsOptional = true;
                }
                else if (arg.StartsWith("--") && arg.Contains("="))
                {
                    commandLineArgs.Add(arg);
                }
                else
                {
                     throw new ArgumentException($"Unknown parameter {arg}");
                }
            }
            else
            {
                configFiles.Add((arg, nextIsOptional));
                nextIsOptional = false;
            }
        }
        return (configFiles, commandLineArgs.ToArray());
    }

    private static void NL() => Console.WriteLine();

    private static void Line(string line, ConsoleColor? color = null)
    {
        if (color is not null)
        {
            Console.ForegroundColor = color.Value;
        }
        Console.WriteLine(line);
        if (color is not null)
        {
            Console.ResetColor();
        }
    }

    private static void Write(string line, ConsoleColor? color = null)
    {
        if (color is not null)
        {
            Console.ForegroundColor = color.Value;
        }
        Console.Write(line);
        if (color is not null)
        {
            Console.ResetColor();
        }
    }

    private static void Line((string str1, string str2)[] lines)
    {
        var pos = lines.Select(l => l.str1.Length).Max() + 1;
        int consoleWidth = Console.WindowWidth;
        foreach (var (str1, str2) in lines)
        {
            Write(str1, ConsoleColor.Yellow);
            Console.CursorLeft = pos;
            var words = str2.Split(' ');
            var line = new StringBuilder(words[0]);
            for (int i = 1; i < words.Length; i++)
            {
                if (line.Length + words[i].Length >= consoleWidth - pos)
                {
                    Line(line.ToString());
                    line.Clear();
                    Console.CursorLeft = pos - 1;
                }
                line.Append(' ' + words[i]);
            }
            if (line.Length > 0)
            {
                Line(line.ToString());
            }
        }
    }
}
