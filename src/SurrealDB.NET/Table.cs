namespace SurrealDB.NET;

#pragma warning disable CA2225

public readonly record struct Table
{
	public required string Name { get; init; }

	public static implicit operator Table(string value)
		=> FromReadOnlySpan(value);

	public static implicit operator Table(ReadOnlySpan<char> value)
		=> FromReadOnlySpan(value);

	public static Table FromReadOnlySpan(ReadOnlySpan<char> span)
	{
		if (span.IsEmpty || span.IsWhiteSpace())
			throw new SurrealException("Surreal Table must be non-empty string consisting of alpha-numeric characters");

		return new Table
		{
			Name = span.ToString(),
		};
	}
}
