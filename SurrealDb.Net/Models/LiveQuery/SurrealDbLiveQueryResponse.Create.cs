namespace SurrealDb.Net.Models.LiveQuery;

public sealed class SurrealDbLiveQueryCreateResponse<T> : SurrealDbLiveQueryResponse
{
    public T Result { get; set; } = default!;

    internal SurrealDbLiveQueryCreateResponse(T result)
    {
        Result = result;
    }
}
