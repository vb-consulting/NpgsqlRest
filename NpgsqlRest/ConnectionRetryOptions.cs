namespace NpgsqlRest;

public class ConnectionRetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts (default: 6, matching EF Core)
    /// </summary>
    public int MaxRetryCount { get; set; } = 6;

    /// <summary>
    /// Maximum delay between retry attempts (default: 30 seconds, matching EF Core)
    /// </summary>
    public TimeSpan MaxRetryDelay{ get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Exponential backoff base multiplier (default: 2.0, matching EF Core)
    /// </summary>
    public double ExponentialBase { get; set; } = 2.0;

    /// <summary>
    /// Random jitter factor (default: 1.1 for 10% jitter, matching EF Core)
    /// </summary>
    public double RandomFactor { get; set; } = 1.1;

    /// <summary>
    /// Base coefficient for delay calculation (default: 1 second, matching EF Core)
    /// </summary>
    public TimeSpan DelayCoefficient { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Additional PostgreSQL error codes to consider retryable beyond the default transient ones
    /// </summary>
    public HashSet<string>? AdditionalErrorCodes { get; set; } = null;

    ///// <summary>
    ///// Creates settings optimized for production use (fewer retries, faster failure)
    ///// </summary>
    //public static ConnectionRetryOpenerSettings Production => new ConnectionRetryOpenerSettings
    //{
    //    MaxRetryCount = 3,
    //    MaxRetryDelay = TimeSpan.FromSeconds(10),
    //    RandomFactor = 1.2 // 20% jitter
    //};

    ///// <summary>
    ///// Creates settings for development/testing (more aggressive retries)
    ///// </summary>
    //public static ConnectionRetryOpenerSettings Development => new ConnectionRetryOpenerSettings
    //{
    //    MaxRetryCount = 8,
    //    MaxRetryDelay = TimeSpan.FromSeconds(60),
    //    RandomFactor = 1.3 // 30% jitter
    //};
}