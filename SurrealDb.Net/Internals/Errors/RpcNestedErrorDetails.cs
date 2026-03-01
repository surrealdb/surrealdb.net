using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Errors;

internal sealed class RpcNestedErrorDetails
{
    [CborProperty("kind")]
    public string Kind { get; internal set; } = string.Empty;

    [CborProperty("details")]
    public ReadOnlyMemory<byte>? Details { get; internal set; }
}
