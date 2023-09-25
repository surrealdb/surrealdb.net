namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from the SurrealDb query is empty.
/// </summary>
public class EmptySurrealDbResponseException : Exception
{
	internal EmptySurrealDbResponseException() : base("The response from the SurrealDb query was empty.")
	{
	}
}
