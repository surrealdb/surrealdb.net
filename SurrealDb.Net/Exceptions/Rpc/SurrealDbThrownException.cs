namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: user-thrown error via THROW in SurrealQL.
/// </summary>
public sealed class SurrealDbThrownException : SurrealDbRpcException
{
    internal SurrealDbThrownException(string message)
        : base(message) { }
}
