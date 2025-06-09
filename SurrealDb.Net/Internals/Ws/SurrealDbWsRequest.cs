using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsRequest
{
    [CborProperty("id")]
    public string Id { get; set; } = string.Empty;

    [CborProperty("method")]
    public string Method { get; set; } = string.Empty;

    [CborProperty("params")]
    [CborIgnoreIfDefault]
    public object?[]? Parameters { get; set; }
}
