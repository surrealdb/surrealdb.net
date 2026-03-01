namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: serialization or deserialization failure.
/// </summary>
public sealed class SurrealDbSerializationExeption : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// True if this is a deserialization error (as opposed to serialization).
    /// </summary>
    public bool IsDeserialization => Kind == "Deserialization";

    internal SurrealDbSerializationExeption(string message, string? kind)
        : base(message)
    {
        Kind = kind;
    }
}
