using System.Text;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Models;

public partial class RecordId
{
    /// <summary>
    /// Creates a new record ID.
    /// </summary>
    /// <param name="recordId">
    /// The record ID.<br /><br />
    ///
    /// <remarks>
    /// Example: `table_name:record_id`
    /// </remarks>
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public RecordId(string recordId)
    {
        _raw = recordId.AsMemory();

        char firstChar = _raw.Span[0];

        char? expectedTableSuffix = firstChar switch
        {
            RecordIdConstants.PREFIX => RecordIdConstants.SUFFIX,
            RecordIdConstants.ALTERNATE_ESCAPE => RecordIdConstants.ALTERNATE_ESCAPE,
            _ => null
        };

        if (expectedTableSuffix.HasValue)
        {
            int suffixIndex = _raw.Span[1..].IndexOf(expectedTableSuffix.Value) + 1;
            if (suffixIndex > 0)
            {
                _separatorIndex =
                    _raw.Span[suffixIndex..].IndexOf(RecordIdConstants.SEPARATOR) + suffixIndex;

                if (_separatorIndex <= suffixIndex)
                    throw new ArgumentException(
                        $"Cannot detect separator on {nameof(RecordId)}",
                        nameof(recordId)
                    );

                _isTableEscaped = true;
                _isIdEscaped = IsStringEscaped(recordId.AsSpan(_separatorIndex + 1));
                return;
            }
        }

        _separatorIndex = _raw.Span.IndexOf(RecordIdConstants.SEPARATOR);

        if (_separatorIndex <= 0)
            throw new ArgumentException(
                $"Cannot detect separator on {nameof(RecordId)}",
                nameof(recordId)
            );

        _isTableEscaped = IsStringEscaped(recordId.AsSpan(0, _separatorIndex));
        _isIdEscaped = IsStringEscaped(recordId.AsSpan(_separatorIndex + 1));

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
    public RecordId(ReadOnlySpan<char> table, ReadOnlySpan<char> id)
    {
        int capacity = table.Length + 1 + id.Length;

        var stringBuilder = new StringBuilder(capacity);
        stringBuilder.Append(table);
        stringBuilder.Append(RecordIdConstants.SEPARATOR);
        stringBuilder.Append(id);

        _raw = stringBuilder.ToString().AsMemory();
        _separatorIndex = table.Length;
        _isTableEscaped = IsStringEscaped(table);
        _isIdEscaped = IsStringEscaped(id);
    }

    internal RecordId(
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

    internal RecordId(
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
