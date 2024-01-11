namespace SurrealDb.Net.Models.LiveQuery;

public sealed class SurrealDbLiveQueryDeleteResponse<T> : SurrealDbLiveQueryResponse
{
    public T Result { get; set; } = default!;

    internal SurrealDbLiveQueryDeleteResponse(T result)
    {
        Result = result;
    }
}
