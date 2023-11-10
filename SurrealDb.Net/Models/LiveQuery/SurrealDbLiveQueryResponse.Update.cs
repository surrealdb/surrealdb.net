namespace SurrealDb.Net.Models.LiveQuery;

public sealed class SurrealDbLiveQueryUpdateResponse<T> : SurrealDbLiveQueryResponse
{
    public T Result { get; set; } = default!;

    internal SurrealDbLiveQueryUpdateResponse(T result)
    {
        Result = result;
    }
}
