namespace SurrealDb.Net.Models;

public partial struct StringRecordId
{
    public static bool operator ==(StringRecordId left, StringRecordId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringRecordId left, StringRecordId right)
    {
        return !(left == right);
    }

    public static explicit operator StringRecordId(string recordId)
    {
        return new(recordId);
    }
}
