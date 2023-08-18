using Superpower;
using SurrealDb.Internals.Constants;
using SurrealDb.Internals.Parsers;

namespace SurrealDb.Models;

public readonly partial struct Duration
{
	/// <summary>
	/// Creates a default Duration (0ns)
	/// </summary>
	public Duration()
	{
		_value = DurationConstants.DefaultDuration;
		_unitValues = new();
	}

	internal Duration(string value)
	{
		_value = value;
		_unitValues = DurationParser.Parser.Parse(value)
			.Where(kv => kv.value != 0)
			.ToDictionary(kv => kv.unit, kv => (int)kv.value);
	}
}
