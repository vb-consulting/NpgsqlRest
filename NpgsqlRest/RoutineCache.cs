using System.Collections.Concurrent;

namespace NpgsqlRest;

public static class RoutineCache
{
    private static readonly ConcurrentDictionary<int, string?> _cache = new();
    private static readonly ConcurrentDictionary<int, string> _originalKeys = new();

    public static bool Get(string key, out string? result)
    {
        var hashedKey = key.GetHashCode();

        if (_cache.TryGetValue(hashedKey, out var value))
        {
            if (_originalKeys.TryGetValue(hashedKey, out var originalKey) && originalKey == key)
            {
                result = value;
                return true;
            }
        }

        result = null;
        return false;
    }

    public static void AddOrUpdate(string key, string? value)
    {
        var hashedKey = key.GetHashCode();
        _cache[hashedKey] = value;
        _originalKeys[hashedKey] = key;
    }
}