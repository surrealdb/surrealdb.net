namespace SurrealDB.NET;

public abstract record SurrealRecord
{
	protected SurrealRecord(Table table)
	{
		Id = new Thing { Table = table };
	}

	public Thing Id { get; init; }
}
