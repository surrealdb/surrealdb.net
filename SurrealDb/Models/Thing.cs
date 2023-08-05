using SurrealDb.Internals.Constants;
using SurrealDb.Internals.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace SurrealDb.Models;

/// <summary>
/// Reflects a record ID (that contains both the table name and table id).
/// </summary>
/// <remarks>
/// Example: `table_name:record_id`
/// </remarks>
public sealed class Thing
{
	private readonly ReadOnlyMemory<char> _raw;
	private readonly int _separatorIndex;
	private readonly bool _isEscaped;

	private int _startIdIndex => _separatorIndex + 1;
	private int _endIdIndex => _raw.Length - 1;

	internal ReadOnlySpan<char> UnescapedIdSpan
	{
		get
		{
			if (_isEscaped)
				return _raw.Span[(_startIdIndex + 1).._endIdIndex];

			return IdSpan;
		}
	}
	internal string UnescapedId => UnescapedIdSpan.ToString();

	[JsonIgnore]
    public ReadOnlySpan<char> TableSpan => _raw.Span[.._separatorIndex];
	[JsonIgnore]
	public ReadOnlySpan<char> IdSpan => _raw.Span[_startIdIndex..];

    public string Table => TableSpan.ToString();
    public string Id => IdSpan.ToString();

    /// <summary>
    /// Creates a new record ID.
    /// </summary>
    /// <param name="thing">
    /// The record ID.<br /><br />
    /// 
    /// <remarks>
    /// Example: `table_name:record_id`
    /// </remarks>
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public Thing(string thing)
    {
        _raw = thing.AsMemory();
        _separatorIndex = _raw.Span.IndexOf(ThingConstants.SEPARATOR);

        if (_separatorIndex <= 0)
        {
            throw new ArgumentException("Cannot detect separator on Thing", nameof(thing));
		}

		_isEscaped = thing[_separatorIndex + 1] == ThingConstants.PREFIX && thing[^1] == ThingConstants.SUFFIX;
	}
    /// <summary>
    /// Creates a new record ID based on the table name and the table id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    public Thing(ReadOnlySpan<char> table, ReadOnlySpan<char> id)
    {
        int capacity = table.Length + 1 + id.Length;

        var stringBuilder = new StringBuilder(capacity);
        stringBuilder.Append(table);
        stringBuilder.Append(ThingConstants.SEPARATOR);
        stringBuilder.Append(id);

        _raw = stringBuilder.ToString().AsMemory();
        _separatorIndex = table.Length;
		_isEscaped = id[0] == ThingConstants.PREFIX && id[^1] == ThingConstants.SUFFIX;
	}

	public override string ToString()
    {
        return _raw.ToString();
	}

	/// <summary>
	/// Creates a new record ID from a table and a genericly typed id.
	/// </summary>
	/// <typeparam name="T">The type of the table id</typeparam>
	/// <param name="table">Table name</param>
	/// <param name="id">Table id</param>
	public static Thing From<T>(ReadOnlySpan<char> table, T id) // TODO : Unit tests
	{
		if (id is string str) // TODO : Check for illegal characters
			return new(table, str);

		var type = typeof(T);

		if (!type.IsPrimitive)
		{
			var serializedId = JsonSerializer.Serialize(
				id,
				SurrealDbSerializerOptions.Default
			);

			char start = serializedId[0];
			char end = serializedId[^1];

			if (start == '"' && end == '"')
				return new(table, CreateEscapedId(serializedId[1..^1]));

			if (start == '{' && end == '}')
				return new(table, serializedId);

			if (start == '[' && end == ']')
				return new(table, serializedId);

			return new(table, CreateEscapedId(serializedId));
		}

		return new(table, id!.ToString());
    }

	private static string CreateEscapedId(string id)
	{
		var stringBuilder = new StringBuilder(id.Length + 2);
		stringBuilder.Append(ThingConstants.PREFIX);
		stringBuilder.Append(id);
		stringBuilder.Append(ThingConstants.SUFFIX);

		return stringBuilder.ToString();
	}
}
