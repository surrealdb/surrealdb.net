namespace SurrealDB.NET;

public readonly record struct Thing
{
	public required string Table { get; init; }

	public string? Id { get; init; }

	public bool IsSpecific => !string.IsNullOrWhiteSpace(Id);

	public static Thing FromReadOnlySpan(ReadOnlySpan<char> span) => span.IndexOf(':') switch
	{
		-1 => new Thing
		{
			Table = span.ToString(),
		},
		var i when span.LastIndexOf(':') == i => new Thing
		{
			Table = span[..i].ToString(),
			Id = span[(i + 1)..].ToString(),
		},
		_ => throw new SurrealException($"Invalid thing format: {span}")
	};

	public static implicit operator Thing(string value)
		=> FromReadOnlySpan(value);

	public static implicit operator Thing(ReadOnlySpan<char> value)
		=> FromReadOnlySpan(value);
}
