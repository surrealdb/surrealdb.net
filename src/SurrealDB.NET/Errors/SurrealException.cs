namespace SurrealDB.NET.Errors;

public sealed class SurrealException : Exception
{
	public SurrealException(string message) : base(message) { }

	internal SurrealException()
	{
	}

	public SurrealException(string message, Exception innerException) : base(message, innerException)
	{
	}
}