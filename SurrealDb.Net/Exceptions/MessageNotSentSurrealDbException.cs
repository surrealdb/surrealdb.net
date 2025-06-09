namespace SurrealDb.Net.Exceptions;

public sealed class MessageNotSentSurrealDbException : SurrealDbException
{
    internal MessageNotSentSurrealDbException()
        : base("Failed to send message.") { }
}
