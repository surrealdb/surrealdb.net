namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: serialization or deserialization failure.
/// </summary>
public sealed class SurrealDbSerializationException : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// True if this is a deserialization error (as opposed to serialization).
    /// </summary>
    public bool IsDeserialization => Kind == "Deserialization";

    internal SurrealDbSerializationException(
        string message,
        string? kind,
        Exception? innerException = null
    )
        : base(message, null, innerException)
    {
        Kind = kind;
    }
}
