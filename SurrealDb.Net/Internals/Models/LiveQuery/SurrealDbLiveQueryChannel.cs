using System.Threading.Channels;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Models.LiveQuery;

internal class SurrealDbLiveQueryChannel
{
    private readonly Channel<ISurrealDbWsLiveResponse> _channel;

    public SurrealDbLiveQueryChannel()
    {
        _channel = Channel.CreateUnbounded<ISurrealDbWsLiveResponse>();
    }

    public async Task WriteAsync(ISurrealDbWsLiveResponse item)
    {
        await _channel.Writer.WriteAsync(item).ConfigureAwait(false);
    }

    public IAsyncEnumerable<ISurrealDbWsLiveResponse> ReadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }
}
