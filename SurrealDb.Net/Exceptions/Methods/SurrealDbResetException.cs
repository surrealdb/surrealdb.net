namespace SurrealDb.Net.Exceptions.Methods;

public sealed class SurrealDbResetException : SurrealDbMethodException
{
    internal SurrealDbResetException(string message)
        : base(message) { }
}
