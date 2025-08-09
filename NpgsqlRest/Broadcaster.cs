using System.Collections.Concurrent;
using System.Threading.Channels;

namespace NpgsqlRest;

public class Broadcaster<T>
{
    private readonly ConcurrentDictionary<Guid, Channel<T>> _channels = new();

    public void Broadcast(T message)
    {
        foreach (var kvp in _channels)
        {
            var writer = kvp.Value.Writer;
            if (!writer.TryWrite(message))
            {
                // Channel is closed, remove it
                _channels.TryRemove(kvp.Key, out _);
            }
        }
    }

    public ChannelReader<T> Subscribe(Guid subscriberId)
    {
        if (_channels.TryRemove(subscriberId, out var existingChannel))
        {
            existingChannel.Writer.Complete();
        }
        var channel = Channel.CreateUnbounded<T>();
        _channels[subscriberId] = channel;
        return channel.Reader;
    }

    public void Unsubscribe(Guid subscriberId)
    {
        if (_channels.TryRemove(subscriberId, out var channel))
        {
            channel.Writer.Complete();
        }
    }
}
