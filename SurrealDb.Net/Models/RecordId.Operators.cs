namespace SurrealDb.Net.Models;

public partial class RecordId
{
    public static bool operator ==(RecordId left, RecordId right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(RecordId left, RecordId right)
    {
        return !(left == right);
    }
}
