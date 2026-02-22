namespace SurrealDb.Net.Exceptions;

public sealed class InvalidSessionException : SurrealDbException
{
    public Guid? SessionId { get; }

    internal InvalidSessionException(Guid? sessionId)
        : base("The provided session is invalid.")
    {
        SessionId = sessionId;
    }
}
