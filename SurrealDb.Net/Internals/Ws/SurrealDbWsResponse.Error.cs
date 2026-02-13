using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsErrorResponse : ISurrealDbWsStandardResponse
{
    public string Id { get; set; } = string.Empty;
    public SurrealDbWsErrorResponseContent Error { get; set; } = new();
}

internal sealed class SurrealDbWsErrorResponseContent
{
    [CborProperty("code")]
    public long Code { get; set; }

    [CborProperty("message")]
    public string Message { get; set; } = string.Empty;

    [CborProperty("kind")]
    public string? Kind { get; set; }
}
