using System.Threading.Channels;
using SurrealDb.Net.Telemetry.Events;

namespace SurrealDb.Net.Telemetry;

public static class SurrealDbTelemetryChannel
{
    private static readonly Channel<ISurrealDbTelemetryEvent> _channel =
        Channel.CreateBounded<ISurrealDbTelemetryEvent>(
            new BoundedChannelOptions(1_000)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            }
        );

    internal static async Task WriteAsync(ISurrealDbTelemetryEvent item)
    {
        await _channel.Writer.WriteAsync(item).ConfigureAwait(false);
    }

    public static IAsyncEnumerable<ISurrealDbTelemetryEvent> ReadAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
