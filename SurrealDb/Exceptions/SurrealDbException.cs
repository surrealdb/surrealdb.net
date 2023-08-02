namespace SurrealDb.Exceptions;

public class SurrealDbException : Exception
{
	internal SurrealDbException(string message) : base(message)
	{
	}
}
