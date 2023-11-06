using System.Collections.Concurrent;

namespace SurrealDb.Net.Internals.Models.LiveQuery;

internal class SurrealDbLiveQueryChannelHolder : ConcurrentBag<SurrealDbLiveQueryChannel>
{
    public string WsEngineId { get; }

    public SurrealDbLiveQueryChannelHolder(string wsEngineId)
    {
        WsEngineId = wsEngineId;
    }
}
