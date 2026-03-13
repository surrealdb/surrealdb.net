namespace SurrealDb.Net.Exceptions.Methods;

public class SurrealDbMethodException : SurrealDbException
{
    public SurrealDbMethodException(string message)
        : base(message) { }
}
