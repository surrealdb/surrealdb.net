namespace SurrealDb.Net.Models.LiveQuery;

public sealed class SurrealDbLiveQueryDeleteResponse : SurrealDbLiveQueryResponse
{
    public Thing Result { get; set; } = default!;

    internal SurrealDbLiveQueryDeleteResponse(Thing result)
    {
        Result = result;
    }
}
