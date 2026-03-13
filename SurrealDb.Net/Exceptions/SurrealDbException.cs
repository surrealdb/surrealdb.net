namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from the SurrealDb query is an unexpected error.
/// </summary>
public abstract class SurrealDbException : Exception
{
    protected internal SurrealDbException(string message)
        : base(message) { }

    protected internal SurrealDbException(string message, Exception? innerException)
        : base(message, innerException) { }
}
