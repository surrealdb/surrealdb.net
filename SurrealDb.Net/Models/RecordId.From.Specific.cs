namespace SurrealDb.Net.Models;

// 💡 Consider Source Generator to generate all possible "RecordId.From" variants (string, int, short, Guid, etc...)
// 💡 Consider client Source Generator to generate custom variants (from custom objects and arrays)

public partial class RecordId
{
    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and a <see cref="string"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RecordId From(string table, string id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        if (id is null)
            throw new ArgumentNullException(nameof(id), "Id should not be null");

        return new RecordIdOfString(table, id);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="int"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RecordId From(string table, int id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        return new RecordIdOf<int>(table, id);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="long"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RecordId From(string table, long id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        return new RecordIdOf<long>(table, id);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="short"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RecordId From(string table, short id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        return new RecordIdOf<short>(table, id);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="byte"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RecordId From(string table, byte id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        return new RecordIdOf<byte>(table, id);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="Guid"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RecordId From(string table, Guid id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        return new RecordIdOf<Guid>(table, id);
    }
}
