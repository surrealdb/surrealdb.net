namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: unexpected or unknown internal error.
/// </summary>
public sealed class SurrealDbInternalException : SurrealDbRpcException
{
    internal SurrealDbInternalException(string message, Exception? innerException = null)
        : base(message, null, innerException) { }
}
