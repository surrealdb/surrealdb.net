namespace SurrealDb.Net.Exceptions.Serialization;

public sealed class SurrealDbSerializationException : SurrealDbException
{
    public SurrealDbSerializationException(string message)
        : base(message) { }
}
