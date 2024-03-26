using Npgsql;

namespace NpgsqlRest;

public class NpgsqlRestParameter : NpgsqlParameter
{
    public string ActualName { get; set; } = default!;
    public TypeDescriptor TypeDescriptor { get; set; } = default!;
}