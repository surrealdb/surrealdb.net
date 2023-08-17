namespace SurrealDb.Models;

public partial class Thing
{
	public static bool operator ==(Thing left, Thing right)
	{
		if (ReferenceEquals(left, right))
			return true;

		if (left is null || right is null)
			return false;

		return left.Equals(right);
	}

	public static bool operator !=(Thing left, Thing right)
	{
		return !(left == right);
	}
}
