namespace SurrealDb.Net.Models;

// 💡 Consider Source Generator to generate all possible implicit operator variants (table or id of type string, int, short, Guid, etc...)
// 💡 Consider client Source Generator to generate custom variants (from objects and arrays), the same way as the RecordId.From source generation
// Note that it is not possible to define both types of a number range (eg. int and uint)

public partial class RecordId
{
    public static implicit operator RecordId((string table, string id) tuple)
    {
        return From(tuple.table, tuple.id);
    }

    public static implicit operator RecordId((string table, int id) tuple)
    {
        return From(tuple.table, tuple.id);
    }

    public static implicit operator RecordId((string table, long id) tuple)
    {
        return From(tuple.table, tuple.id);
    }

    public static implicit operator RecordId((string table, short id) tuple)
    {
        return From(tuple.table, tuple.id);
    }

    public static implicit operator RecordId((string table, byte id) tuple)
    {
        return From(tuple.table, tuple.id);
    }

    public static implicit operator RecordId((string table, Guid id) tuple)
    {
        return From(tuple.table, tuple.id);
    }
}
