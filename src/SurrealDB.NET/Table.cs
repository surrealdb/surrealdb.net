namespace SurrealDB.NET;

public readonly record struct Table
{
	public required string Name { get; init; }

	public static implicit operator Table(string value)
		=> FromReadOnlySpan(value);

	public static implicit operator Table(ReadOnlySpan<char> value)
		=> FromReadOnlySpan(value);

	public static Table FromReadOnlySpan(ReadOnlySpan<char> span)
	{
		// TODO: Validate the table name

		return new Table
		{
			Name = span.ToString(),
		};
	}
}
