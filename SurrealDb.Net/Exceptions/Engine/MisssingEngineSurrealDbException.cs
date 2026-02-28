namespace SurrealDb.Net.Exceptions;

public sealed class MisssingEngineSurrealDbException : EngineSurrealDbException
{
    internal MisssingEngineSurrealDbException()
        : base("No underlying engine is started.") { }
}
