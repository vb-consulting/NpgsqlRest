using Npgsql;

namespace NpgsqlRest;

public class NpgsqlRestBatch : NpgsqlBatch
{
    private NpgsqlBatch NpgsqlBatchClone()
    {
#pragma warning disable CS8603 // Possible null reference return.
        return MemberwiseClone() as NpgsqlBatch;
#pragma warning restore CS8603 // Possible null reference return.
    }

    private static readonly NpgsqlRestBatch _instanceCache = new();

    public static NpgsqlBatch Create(NpgsqlConnection connection)
    {
        var result = _instanceCache.NpgsqlBatchClone();
        result.Connection = connection;
        return result;
    }
}