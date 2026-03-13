namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient
{
    protected Guid? TransactionId { get; init; }
}
