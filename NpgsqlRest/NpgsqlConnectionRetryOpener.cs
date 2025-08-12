using Npgsql;

namespace NpgsqlRest;

public static class NpgsqlConnectionRetryOpener
{
    private static readonly Random _random = new Random();

    public static void Open(NpgsqlConnection connection, ConnectionRetryOptions settings, ILogger? logger = null)
    {
        var exceptionsEncountered = new List<Exception>();
        for (int attempt = 0; attempt <= settings.MaxRetryCount; attempt++)
        {
            try
            {
                logger?.LogDebug("Attempting to open PostgreSQL connection (attempt {Attempt}/{MaxAttempts})",
                    attempt + 1, settings.MaxRetryCount + 1);

                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    connection.Close();
                }

                connection.Open();

                logger?.LogDebug("Successfully opened PostgreSQL connection on attempt {Attempt}", attempt + 1);
                return;
            }
            catch (Exception ex) when (ShouldRetryOn(ex, settings))
            {
                exceptionsEncountered.Add(ex);

                if (attempt < settings.MaxRetryCount)
                {
                    var delay = GetNextDelay(exceptionsEncountered.Count - 1, settings);

                    if (delay.HasValue)
                    {
                        logger?.LogWarning("Failed to open PostgreSQL connection on attempt {Attempt}. Retrying in {Delay}ms. Error: {Error}",
                            attempt + 1, delay.Value.TotalMilliseconds, ex.Message);

                        Thread.Sleep(delay.Value);
                    }
                    else
                    {
                        logger?.LogError(ex, "Failed to open PostgreSQL connection after {TotalAttempts} attempts", attempt + 1);
                        ThrowRetryExhaustedException(exceptionsEncountered);
                    }
                }
                else
                {
                    logger?.LogError(ex, "Failed to open PostgreSQL connection after {TotalAttempts} attempts", attempt + 1);
                    ThrowRetryExhaustedException(exceptionsEncountered);
                }
            }
            catch (Exception ex)
            {
                // Non-retryable exception
                logger?.LogError(ex, "Non-retryable error occurred while opening PostgreSQL connection: {Error}", ex.Message);
                throw;
            }
        }
    }

    public static async Task OpenAsync(
        NpgsqlConnection connection,
        ConnectionRetryOptions settings,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var exceptionsEncountered = new List<Exception>();
        for (int attempt = 0; attempt <= settings.MaxRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger?.LogDebug("Attempting to open connection (attempt {Attempt}/{MaxAttempts})",
                    attempt + 1, settings.MaxRetryCount + 1);

                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }

                await connection.OpenAsync(cancellationToken);

                logger?.LogDebug("Successfully opened connection on attempt {Attempt}", attempt + 1);
                return;
            }
            catch (Exception ex) when (ShouldRetryOn(ex, settings) && !cancellationToken.IsCancellationRequested)
            {
                exceptionsEncountered.Add(ex);

                if (attempt < settings.MaxRetryCount)
                {
                    var delay = GetNextDelay(exceptionsEncountered.Count - 1, settings);

                    if (delay.HasValue)
                    {
                        logger?.LogWarning("Failed to open connection on attempt {Attempt}. Retrying in {Delay}ms. Error: {Error}",
                            attempt + 1, delay.Value.TotalMilliseconds, ex.Message);

                        await Task.Delay(delay.Value, cancellationToken);
                    }
                    else
                    {
                        logger?.LogError(ex, "Failed to open connection after {TotalAttempts} attempts", attempt + 1);
                        ThrowRetryExhaustedException(exceptionsEncountered);
                    }
                }
                else
                {
                    logger?.LogError(ex, "Failed to open connection after {TotalAttempts} attempts", attempt + 1);
                    ThrowRetryExhaustedException(exceptionsEncountered);
                }
            }
            catch (Exception ex)
            {
                // Non-retryable exception or cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    logger?.LogDebug("PostgreSQL connection opening was cancelled");
                    throw;
                }

                logger?.LogError(ex, "Non-retryable error occurred while opening connection: {Error}", ex.Message);
                throw;
            }
        }
    }

    private static bool ShouldRetryOn(Exception exception, ConnectionRetryOptions settings)
    {
        if (exception is NpgsqlException npgsqlException)
        {
            if (npgsqlException.IsTransient)
            {
                return true;
            }

            if (npgsqlException.SqlState is null)
            {
                return true;
            }

            // Check additional error codes if specified
            if (settings.AdditionalErrorCodes?.Contains(npgsqlException.SqlState) == true)
            {
                return true;
            }

            // Default PostgreSQL transient error codes (matching EF Core pattern)
            return npgsqlException.SqlState switch
            {
                // Connection failure codes
                "08000" or "08003" or "08006" or "08001" or "08004" => true,
                // Lock not available
                "55P03" => true,
                // Object in use
                "55006" => true,
                // Too many connections
                "53300" => true,
                // Cannot connect now
                "57P03" => true,
                // Serialization failure (can be retried)
                "40001" => true,
                _ => false
            };
        }

        // Handle other exception types (matching EF Core pattern)
        return exception switch
        {
            TimeoutException => true,
            System.Net.Sockets.SocketException => true,
            System.Net.NetworkInformation.NetworkInformationException => true,
            TaskCanceledException => false, // Don't retry cancellation
            OperationCanceledException => false, // Don't retry cancellation
            _ => false
        };
    }

    private static TimeSpan? GetNextDelay(int currentRetryCount, ConnectionRetryOptions settings)
    {
        if (currentRetryCount < settings.MaxRetryCount)
        {
            // EF Core's exact exponential backoff formula with jitter (including divisor for geometric series)
            var delta = (Math.Pow(settings.ExponentialBase, currentRetryCount) - 1.0)
                / (settings.ExponentialBase - 1.0)
                * (1.0 + _random.NextDouble() * (settings.RandomFactor - 1.0));

            var delay = Math.Min(
                settings.DelayCoefficient.TotalMilliseconds * delta,
                settings.MaxRetryDelay.TotalMilliseconds);

            return TimeSpan.FromMilliseconds(Math.Max(0, delay));
        }
        return null;
    }

    private static void ThrowRetryExhaustedException(List<Exception> exceptionsEncountered)
    {
        throw new NpgsqlRetryExhaustedException(
            exceptionsEncountered.Count,
            exceptionsEncountered.ToArray(),
            $"Failed to open PostgreSQL connection after {exceptionsEncountered.Count} attempts. See inner exception for details.");
    }
}

public class NpgsqlRetryExhaustedException : Exception
{
    public int TotalAttempts { get; }
    public Exception[] AttemptExceptions { get; }

    public NpgsqlRetryExhaustedException(int totalAttempts, Exception[] attemptExceptions, string message)
        : base(message, attemptExceptions?.Length > 0 ? attemptExceptions[^1] : null)
    {
        TotalAttempts = totalAttempts;
        AttemptExceptions = attemptExceptions ?? Array.Empty<Exception>();
    }
}
