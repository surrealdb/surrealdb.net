namespace SurrealDb.Net.Exceptions.Methods;

public sealed class SurrealDbConnectException : SurrealDbMethodException
{
    internal SurrealDbConnectException(string message)
        : base(message) { }
}
