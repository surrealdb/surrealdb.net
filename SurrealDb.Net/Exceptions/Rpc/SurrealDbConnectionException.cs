namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: connection issues.
/// </summary>
public sealed class SurrealDbConnectionException : SurrealDbRpcException
{
    public string? Kind { get; }

    internal SurrealDbConnectionException(string message, string? kind)
        : base(message)
    {
        Kind = kind;
    }
}
