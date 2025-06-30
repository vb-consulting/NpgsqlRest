namespace NpgsqlRest;

public abstract class Broadcast
{
    public const long InitialVersion = long.MinValue;
}

public class Broadcast<T> : IDisposable
{
    private readonly ReaderWriterLockSlim _lock;
    private readonly ManualResetEventSlim _messageAvailable;
    private T _payload;
    private long _version;

    public Broadcast()
    {
        _lock = new ReaderWriterLockSlim();
        _messageAvailable = new ManualResetEventSlim(false);
        _payload = default!;
        _version = Broadcast.InitialVersion;
    }

    public void Write(T message)
    {
        _lock.EnterWriteLock();
        try
        {
            _payload = message;
            _version++;
            _messageAvailable.Set(); // Signal waiting consumers
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool TryWaitForNewMessage(long lastVersion, CancellationToken cancellationToken, out (T Payload, long Version) result)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _lock.EnterReadLock();
        try
        {
            if (_version > lastVersion)
            {
                result = (_payload, _version);
                return true;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _messageAvailable.Wait(cancellationToken);
        _lock.EnterReadLock();
        try
        {
            if (_version > lastVersion)
            {
                result = (_payload, _version);
                return true;
            }
            result = (default!, _version);
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        _messageAvailable.Dispose();
    }
}