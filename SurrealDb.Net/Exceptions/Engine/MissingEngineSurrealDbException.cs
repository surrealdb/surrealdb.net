namespace SurrealDb.Net.Exceptions;

public sealed class MissingEngineSurrealDbException : EngineSurrealDbException
{
    internal MissingEngineSurrealDbException()
        : base("No underlying engine is started.") { }
}
