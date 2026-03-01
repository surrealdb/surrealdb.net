namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from a SurrealDB method is a known error.
/// </summary>
public abstract class SurrealDbRpcException : SurrealDbException
{
    protected internal SurrealDbRpcException(string message)
        : base(message) { }
}
