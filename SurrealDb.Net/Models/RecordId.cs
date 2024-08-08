namespace SurrealDb.Net.Models;

/// <summary>
/// Reflects a record ID (that contains both the table name and table id).
/// </summary>
/// <remarks>
/// Example: `table_name:record_id`
/// </remarks>
public partial class RecordId : IEquatable<RecordId>
{
    public bool Equals(RecordId? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (_isIdEscaped != other._isIdEscaped)
            return false;

        if (_isTableEscaped != other._isTableEscaped)
            return false;

        bool isSameTable = UnescapedTableSpan.SequenceEqual(other.UnescapedTableSpan);
        if (!isSameTable)
            return false;

        bool isSameRecordId = UnescapedIdSpan.SequenceEqual(other.UnescapedIdSpan);
        return isSameRecordId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is RecordId other)
            return Equals(other);

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_raw);
    }

    public override string ToString()
    {
        return _raw.ToString();
    }
}
