using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;
using System.Text;

namespace SurrealDb.Net.Models;

public partial class Thing
{
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
            throw new ArgumentException("Cannot detect separator on Thing", nameof(thing));

        _isTableEscaped = IsStringEscaped(thing.AsSpan(0, _separatorIndex));
        _isIdEscaped = IsStringEscaped(thing.AsSpan(_separatorIndex + 1));
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
        _isTableEscaped = IsStringEscaped(table);
        _isIdEscaped = IsStringEscaped(id);
    }

    internal Thing(
        ReadOnlySpan<char> table,
        SpecialRecordPartType specialTableType,
        ReadOnlySpan<char> id,
        SpecialRecordPartType specialRecordIdType
    )
        : this(table, id)
    {
        _specialTableType = specialTableType;
        _specialRecordIdType = specialRecordIdType;
    }
}
