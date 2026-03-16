namespace SurrealDb.Net.Exceptions.Response;

public sealed class UnknownResponseTypeException : SurrealDbException
{
    internal UnknownResponseTypeException()
        : base("Unknown response type") { }
}
