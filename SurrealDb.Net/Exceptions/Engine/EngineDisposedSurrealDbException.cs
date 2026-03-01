namespace SurrealDb.Net.Exceptions;

public sealed class EngineDisposedSurrealDbException : EngineSurrealDbException
{
    internal EngineDisposedSurrealDbException()
        : base("The underlying engine of the SurrealDB client has been disposed.") { }
}
