namespace SurrealDb.Net.Internals.Ws;

internal interface ISurrealDbWsResponse { }

internal interface ISurrealDbWsStandardResponse : ISurrealDbWsResponse
{
    public string Id { get; set; }
}

internal interface ISurrealDbWsLiveResponse : ISurrealDbWsResponse { }
