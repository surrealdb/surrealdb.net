using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsErrorResponse : ISurrealDbWsStandardResponse
{
    public string Id { get; set; } = string.Empty;
    public SurrealDbWsErrorResponseContent Error { get; set; } = new();
}

internal class SurrealDbWsErrorResponseContent
{
    [CborProperty("code")]
    public long Code { get; set; }

    [CborProperty("message")]
    public string Message { get; set; } = string.Empty;
}
