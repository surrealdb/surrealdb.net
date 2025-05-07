using SurrealDb.Net.Models.LiveQuery;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsClosedLiveResponse : ISurrealDbWsLiveResponse
{
    public SurrealDbLiveQueryClosureReason Reason { get; set; }
}
