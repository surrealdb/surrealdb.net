namespace SurrealDB.NET;

public sealed class SurrealException : Exception
{
    private readonly SurrealError _error;

    public SurrealException(string message) : base(message) { }

    public SurrealException(SurrealError error) : base(error.Message)
    {
        _error = error;
    }

    internal SurrealException()
    {
    }

    public SurrealException(string message, Exception innerException) : base(message, innerException)
    {
    }
}