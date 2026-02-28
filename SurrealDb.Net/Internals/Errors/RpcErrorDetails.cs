using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Errors;

internal sealed class RpcErrorDetails
{
    [CborProperty("kind")]
    public string Kind { get; internal set; } = string.Empty;

    [CborProperty("details")]
    public RpcNestedErrorDetails? Details { get; internal set; }
}
