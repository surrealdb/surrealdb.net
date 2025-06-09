#if NET9_0_OR_GREATER
using System.Threading.Channels;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsSendRequestChannel
{
    private readonly Channel<SurrealDbWsSendRequest> _channel;

    internal SurrealDbWsSendRequestChannel()
    {
        _channel = Channel.CreateUnboundedPrioritized(
            new UnboundedPrioritizedChannelOptions<SurrealDbWsSendRequest>
            {
                Comparer = new SurrealDbWsSendRequestPriorityComparer(),
            }
        );
    }

    internal async Task WriteAsync(SurrealDbWsSendRequest item, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
    }

    internal IAsyncEnumerable<SurrealDbWsSendRequest> ReadAllAsync(
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
#endif
