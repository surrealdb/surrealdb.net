namespace SurrealDB.NET;

public readonly record struct SurrealLiveQueryId
{
    public Guid Id { get; }

    internal SurrealLiveQueryId(Guid id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}
