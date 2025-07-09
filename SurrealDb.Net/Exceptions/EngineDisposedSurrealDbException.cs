namespace SurrealDb.Net.Exceptions;

public sealed class EngineDisposedSurrealDbException : SurrealDbException
{
    internal EngineDisposedSurrealDbException()
        : base("The underlying engine of the SurrealDB client has been disposed.") { }
}
