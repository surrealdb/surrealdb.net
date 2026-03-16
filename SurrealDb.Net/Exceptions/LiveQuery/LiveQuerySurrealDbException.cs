namespace SurrealDb.Net.Exceptions.LiveQuery;

public sealed class LiveQuerySurrealDbException : SurrealDbException
{
    internal LiveQuerySurrealDbException(string message)
        : base(message) { }
}
