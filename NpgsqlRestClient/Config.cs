namespace NpgsqlRestClient;

public static class Config
{
    public static IConfigurationRoot Cfg { get; private set; } = null!;
    public static IConfigurationSection NpgsqlRestCfg { get; private set; } = null!;
    public static IConfigurationSection AuthCfg { get; private set; } = null!;
    public static string CurrentDir => Directory.GetCurrentDirectory();

    public static void Build(string[] args)
    {
        var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

        if (args.Length > 0)
        {
            foreach (var (fileName, optional) in Arguments.EnumerateConfigFiles(args))
            {
                configBuilder.AddJsonFile(Path.GetFullPath(fileName, CurrentDir), optional: optional);
            }
            Cfg = configBuilder.Build();
        }
        else
        {
            Cfg = configBuilder
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
        }

        NpgsqlRestCfg = Cfg.GetSection("NpgsqlRest");
        AuthCfg = NpgsqlRestCfg.GetSection("AuthenticationOptions");
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
}
