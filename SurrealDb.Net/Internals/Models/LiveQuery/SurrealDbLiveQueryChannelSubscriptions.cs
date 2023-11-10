using System.Collections.Concurrent;

namespace SurrealDb.Net.Internals.Models.LiveQuery;

internal class SurrealDbLiveQueryChannelSubscriptions : ConcurrentBag<SurrealDbLiveQueryChannel>
{
    public string WsEngineId { get; }

    public SurrealDbLiveQueryChannelSubscriptions(string wsEngineId)
    {
        WsEngineId = wsEngineId;
    }
}
