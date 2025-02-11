using System.Text.Json;
using System.Text.Json.Nodes;

namespace NpgsqlRestClient;

public static class Config
{
    public static IConfigurationRoot Cfg { get; private set; } = null!;
    public static IConfigurationSection NpgsqlRestCfg { get; private set; } = null!;
    public static IConfigurationSection ConnectionSettingsCfg { get; private set; } = null!;
    public static bool UseConnectionApplicationNameWithUsername { get; private set; }
    public static IConfigurationSection AuthCfg { get; private set; } = null!;
    public static string CurrentDir => Directory.GetCurrentDirectory();

    public static void Build(string[] args)
    {
        var tempBuilder = new ConfigurationBuilder();
        IConfigurationRoot tempCfg;

        var (configFiles, commandLineArgs) = Arguments.BuildFromArgs(args);

        if (configFiles.Count > 0)
        {
            foreach (var (fileName, optional) in configFiles)
            {
                tempBuilder.AddJsonFile(Path.GetFullPath(fileName, CurrentDir), optional: optional);
            }
            tempCfg = tempBuilder.Build();
        }
        else
        {
            tempCfg = tempBuilder
                .AddJsonFile(Path.GetFullPath("appsettings.json", CurrentDir), optional: true)
                .AddJsonFile(Path.GetFullPath("appsettings.Development.json", CurrentDir), optional: true)
                .Build();
        }
        var cfgCfg = tempCfg.GetSection("Config");
        IConfigurationBuilder configBuilder = new ConfigurationBuilder();
        var useEnv = cfgCfg != null && GetConfigBool("AddEnvironmentVariables", cfgCfg);

        if (configFiles.Count > 0)
        {
            foreach (var (fileName, optional) in configFiles)
            {
                configBuilder.AddJsonFile(Path.GetFullPath(fileName, CurrentDir), optional: optional);
            }
            if (useEnv)
            {
                configBuilder.AddEnvironmentVariables();
            }
            configBuilder.AddCommandLine(commandLineArgs);
            Cfg = configBuilder.Build();
        }
        else
        {
            if (useEnv)
            {
                Cfg = configBuilder
                    .AddJsonFile(Path.GetFullPath("appsettings.json", CurrentDir), optional: true)
                    .AddJsonFile(Path.GetFullPath("appsettings.Development.json", CurrentDir), optional: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(commandLineArgs)
                    .Build();
            }
            else
            {
                Cfg = configBuilder
                    .AddJsonFile(Path.GetFullPath("appsettings.json", CurrentDir), optional: true)
                    .AddJsonFile(Path.GetFullPath("appsettings.Development.json", CurrentDir), optional: true)
                    .AddCommandLine(commandLineArgs)
                    .Build();
            }
        }

        NpgsqlRestCfg = Cfg.GetSection("NpgsqlRest");
        AuthCfg = NpgsqlRestCfg.GetSection("AuthenticationOptions");
        ConnectionSettingsCfg = Cfg.GetSection("ConnectionSettings");

        UseConnectionApplicationNameWithUsername = GetConfigBool("UseJsonApplicationName", ConnectionSettingsCfg);
    }

    public static bool Exists(this IConfigurationSection? section)
    {
        if (section is null)
        {
            return false;
        }
        if (section.GetChildren().Any() is false) 
        {
            return false;
        }
        return true;
    }

    public static bool GetConfigBool(string key, IConfiguration? subsection = null, bool defaultVal = false)
    {
        var section = subsection?.GetSection(key) ?? Cfg.GetSection(key);
        if (string.IsNullOrEmpty(section?.Value))
        {
            return defaultVal;
        }
        return string.Equals(section?.Value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetConfigStr(string key, IConfiguration? subsection = null)
    {
        var section = subsection?.GetSection(key) ?? Cfg.GetSection(key);
        return string.IsNullOrEmpty(section?.Value) ? null : section.Value;
    }

    public static int? GetConfigInt(string key, IConfiguration? subsection = null)
    {
        var section = subsection?.GetSection(key) ?? Cfg.GetSection(key);
        if (section?.Value is null)
        {
            return null;
        }
        if (int.TryParse(section.Value, out var value))
        {
            return value;
        }
        return null;
    }

    public static T? GetConfigEnum<T>(string key, IConfiguration? subsection = null)
    {
        var section = subsection?.GetSection(key) ?? Cfg.GetSection(key);
        if (string.IsNullOrEmpty(section?.Value))
        {
            return default;
        }
        return GetEnum<T>(section?.Value);
    }

    public static T? GetEnum<T>(string? value)
    {
        if (value is null)
        {
            return default;
        }
        var type = typeof(T);
        var nullable = Nullable.GetUnderlyingType(type);
        var names = Enum.GetNames(nullable ?? type);
        foreach (var name in names)
        {
            if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
            {
                return (T)Enum.Parse(nullable ?? type, name);
            }
        }
        return default;
    }

    public static IEnumerable<string>? GetConfigEnumerable(string key, IConfiguration? subsection = null)
    {
        var section = subsection is not null ? subsection?.GetSection(key) : Cfg.GetSection(key);
        if (section.Exists() is false)
        {
            return null;
        }
        var children = section?.GetChildren().ToArray();
        if (children is null || (children.Length == 0 && section?.Value == ""))
        {
            return null;
        }
        return children.Select(c => c.Value ?? "");
    }

    public static T? GetConfigFlag<T>(string key, IConfiguration? subsection = null)
    {
        var array = GetConfigEnumerable(key, subsection)?.ToArray();
        if (array is null)
        {
            return default;
        }

        var type = typeof(T);
        var nullable = Nullable.GetUnderlyingType(type);
        var names = Enum.GetNames(nullable ?? type);

        T? result = default;
        foreach (var value in array)
        {
            foreach (var name in names)
            {
                if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
                {
                    var e = (T)Enum.Parse(nullable ?? type, name);
                    if (result is null)
                    {
                        result = e;
                    }
                    else
                    {
                        result = (T)Enum.ToObject(type, Convert.ToInt32(result) | Convert.ToInt32(e));
                    }
                }
            }
        }
        return result;
    }

    public static string Serialize()
    {
        var json = SerializeConfig(Cfg);
        return json?.ToJsonString(new JsonSerializerOptions() { WriteIndented = true }) ?? "{}";
    }

    private static JsonNode? SerializeConfig(IConfiguration config)
    {
        JsonObject obj = [];

        foreach (var child in config.GetChildren())
        {
            if (child.Path.EndsWith(":0"))
            {
                var arr = new JsonArray();

                foreach (var arrayChild in config.GetChildren())
                {
                    arr.Add(SerializeConfig(arrayChild));
                }

                return arr;
            }
            else
            {
                obj.Add(child.Key, SerializeConfig(child));
            }
        }

        if (obj.Count == 0 && config is IConfigurationSection section)
        {
            if (bool.TryParse(section.Value, out bool boolean))
            {
                return JsonValue.Create(boolean);
            }
            else if (decimal.TryParse(section.Value, out decimal real))
            {
                return JsonValue.Create(real);
            }
            else if (long.TryParse(section.Value, out long integer))
            {
                return JsonValue.Create(integer);
            }
            if (section.Path.StartsWith("ConnectionStrings:"))
            {
                return JsonValue.Create(string.Join(';', 
                    section?.Value?.Split(';')?.Where(p => p.StartsWith("password", StringComparison.OrdinalIgnoreCase) is false) ?? []));
            }
            return JsonValue.Create(section.Value);
        }

        return obj;
    }
}
