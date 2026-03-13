namespace SurrealDb.Net.Exceptions.Embedded;

public sealed class SurrealDbEmbeddedException : SurrealDbException
{
    public SurrealDbEmbeddedException(string message)
        : base(message) { }
}
