using System.Threading.Channels;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Models.LiveQuery;

public sealed class SurrealDbLiveQueryChannel
{
    private readonly Channel<ISurrealDbWsLiveResponse> _channel;

    internal SurrealDbLiveQueryChannel()
    {
        _channel = Channel.CreateUnbounded<ISurrealDbWsLiveResponse>();
    }

    internal async Task WriteAsync(ISurrealDbWsLiveResponse item)
    {
        await _channel.Writer.WriteAsync(item).ConfigureAwait(false);
    }

    internal IAsyncEnumerable<ISurrealDbWsLiveResponse> ReadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    internal void Complete()
    {
        _channel.Writer.Complete();
    }
}
