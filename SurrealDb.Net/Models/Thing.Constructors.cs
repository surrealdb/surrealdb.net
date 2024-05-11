using System.Text;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

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
    [JsonConstructor]
    public Thing(string thing)
    {
        _raw = thing.AsMemory();

        char firstChar = _raw.Span[0];

        char? expectedTableSuffix = firstChar switch
        {
            ThingConstants.PREFIX => ThingConstants.SUFFIX,
            ThingConstants.ALTERNATE_ESCAPE => ThingConstants.ALTERNATE_ESCAPE,
            _ => null
        };

        if (expectedTableSuffix.HasValue)
        {
            int suffixIndex = _raw.Span[1..].IndexOf(expectedTableSuffix.Value) + 1;
            if (suffixIndex > 0)
            {
                _separatorIndex =
                    _raw.Span[suffixIndex..].IndexOf(ThingConstants.SEPARATOR) + suffixIndex;

                if (_separatorIndex <= suffixIndex)
                    throw new ArgumentException("Cannot detect separator on Thing", nameof(thing));

                _isTableEscaped = true;
                _isIdEscaped = IsStringEscaped(thing.AsSpan(_separatorIndex + 1));
                return;
            }
        }

        _separatorIndex = _raw.Span.IndexOf(ThingConstants.SEPARATOR);

        if (_separatorIndex <= 0)
            throw new ArgumentException("Cannot detect separator on Thing", nameof(thing));

        _isTableEscaped = IsStringEscaped(thing.AsSpan(0, _separatorIndex));
        _isIdEscaped = IsStringEscaped(thing.AsSpan(_separatorIndex + 1));

        if (!_isIdEscaped && IdSpan.Length >= 2)
        {
            char start = IdSpan[0];
            char end = IdSpan[^1];

            bool isJsonObject = start == '{' && end == '}';
            if (isJsonObject)
            {
                _specialRecordIdType = SpecialRecordPartType.JsonObject;
            }

            bool isJsonArray = start == '[' && end == ']';
            if (isJsonArray)
            {
                _specialRecordIdType = SpecialRecordPartType.JsonArray;
            }
        }
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

    internal Thing(
        ReadOnlySpan<char> table,
        SpecialRecordPartType specialTableType,
        ReadOnlyMemory<byte> id
    )
    {
        _specialTableType = specialTableType;
        _specialRecordIdType = SpecialRecordPartType.SerializedCbor;

        _raw = table.ToString().AsMemory();
        _separatorIndex = table.Length;
        _isTableEscaped = IsStringEscaped(table);
        _serializedCborId = id;
    }
}
