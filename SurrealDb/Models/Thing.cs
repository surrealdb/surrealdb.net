namespace SurrealDb.Models;

/// <summary>
/// Reflects a record ID (that contains both the table name and table id).
/// </summary>
/// <remarks>
/// Example: `table_name:record_id`
/// </remarks>
public partial class Thing : IEquatable<Thing>
{
	public bool Equals(Thing? other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		bool isSameTable = TableSpan.SequenceEqual(other.TableSpan);

		if (!isSameTable)
			return false;

		if (_isEscaped != other._isEscaped)
			return false;

		bool isSameRecordId = UnescapedIdSpan.SequenceEqual(other.UnescapedIdSpan);
		return isSameRecordId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is Thing other)
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
