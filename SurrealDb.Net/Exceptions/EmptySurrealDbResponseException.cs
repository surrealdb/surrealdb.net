namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from the SurrealDb query is empty.
/// </summary>
public sealed class EmptySurrealDbResponseException : SurrealDbException
{
    internal EmptySurrealDbResponseException()
        : base("The response from the SurrealDb query was empty.") { }
}
