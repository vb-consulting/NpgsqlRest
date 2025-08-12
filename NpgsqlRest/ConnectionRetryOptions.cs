namespace NpgsqlRest;

public class ConnectionRetryOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>
    /// Maximum delay between retry attempts
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Exponential backoff base multiplier
    /// </summary>
    public double ExponentialBase { get; set; } = 2.0;

    /// <summary>
    /// Random jitter factor (1.1 for 10% jitter, 1.2 for 20% jitter, 1.3 for 30% jitter, etc)
    /// </summary>
    public double RandomFactor { get; set; } = 1.1;

    /// <summary>
    /// Base coefficient for delay calculation.
    /// </summary>
    public TimeSpan DelayCoefficient { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Additional PostgreSQL error codes to consider retryable beyond the default transient ones
    /// </summary>
    public HashSet<string>? AdditionalErrorCodes { get; set; } = null;
}