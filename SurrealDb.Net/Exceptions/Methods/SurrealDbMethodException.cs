namespace SurrealDb.Net.Exceptions.Methods;

public class SurrealDbMethodException : SurrealDbException
{
    public SurrealDbMethodException(string message)
        : base(message) { }

    protected internal SurrealDbMethodException(string message, Exception? innerException)
        : base(message, innerException) { }
}
