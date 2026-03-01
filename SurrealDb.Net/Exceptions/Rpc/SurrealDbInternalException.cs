namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: unexpected or unknown internal error.
/// </summary>
public sealed class SurrealDbInternalException : SurrealDbRpcException
{
    internal SurrealDbInternalException(string message)
        : base(message) { }
}
