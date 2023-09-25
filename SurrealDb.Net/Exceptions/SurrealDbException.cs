namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from the SurrealDb query is an unexpected error.
/// </summary>
public class SurrealDbException : Exception
{
	internal SurrealDbException(string message) : base(message)
	{
	}
}
