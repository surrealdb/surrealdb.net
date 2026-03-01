namespace SurrealDb.Net.Exceptions;

public abstract class EngineSurrealDbException : SurrealDbException
{
    protected internal EngineSurrealDbException(string message)
        : base(message) { }
}
