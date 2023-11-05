namespace SurrealDb.Net.Models.LiveQuery;

public sealed class SurrealDbLiveQueryCloseResponse : SurrealDbLiveQueryResponse
{
    public SurrealDbLiveQueryClosureReason Reason { get; set; }

    internal SurrealDbLiveQueryCloseResponse(SurrealDbLiveQueryClosureReason reason)
    {
        Reason = reason;
    }
}
