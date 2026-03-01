namespace SurrealDb.Net.Exceptions.Methods;

public sealed class SurrealDbExportException : SurrealDbMethodException
{
    internal SurrealDbExportException(string message)
        : base(message) { }
}
