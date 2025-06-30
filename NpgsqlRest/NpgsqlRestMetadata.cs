namespace NpgsqlRest;

public class NpgsqlRestMetadataEntry
{
    internal NpgsqlRestMetadataEntry(RoutineEndpoint endpoint, IRoutineSourceParameterFormatter formatter, string key)
    {
        Endpoint = endpoint;
        Formatter = formatter;
        Key = key;
    }
    public RoutineEndpoint Endpoint { get; }
    public IRoutineSourceParameterFormatter Formatter { get; }
    public string Key { get; }
}

public class NpgsqlRestMetadata
{
    internal NpgsqlRestMetadata(
        Dictionary<string, NpgsqlRestMetadataEntry> entries,
        Dictionary<string, NpgsqlRestMetadataEntry> overloads)
    {
        Entries = entries;
        Overloads = overloads;
    }

    public Dictionary<string, NpgsqlRestMetadataEntry> Entries { get; }
    public Dictionary<string, NpgsqlRestMetadataEntry> Overloads { get; }
    public bool HasStreamingEvents { get; set; } = false;
}
