namespace SurrealDb.Net.Exceptions;

public sealed class SurrealDbTransactionException : SurrealDbException
{
    internal SurrealDbTransactionException()
        : base("The SurrealDB transaction has already been committed or canceled.") { }
}
