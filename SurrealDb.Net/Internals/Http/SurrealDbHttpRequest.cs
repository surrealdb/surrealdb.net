using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Http;

internal sealed class SurrealDbHttpRequest
{
    [CborProperty("method")]
    public string Method { get; set; } = string.Empty;

    [CborProperty("params")]
    [CborIgnoreIfDefault]
    public object?[]? Parameters { get; set; }

    [CborProperty("session")]
    [CborIgnoreIfDefault]
    public Guid? SessionId { get; set; }
}
