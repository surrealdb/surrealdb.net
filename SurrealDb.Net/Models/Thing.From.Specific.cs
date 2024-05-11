using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Models;

// 💡 Non-generic "Thing.From" methods that are AOT compatible
// 💡 Consider Source Generator to generate all possible "Thing.From" variants (string, int, short, Guid, etc...)
// 💡 Consider client Source Generator to generate custom variants (from objects and arrays)

public partial class Thing
{
    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and a <see cref="string"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, string id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        if (id is null)
            throw new ArgumentNullException(nameof(id), "Id should not be null");

        var tablePart = ExtractStringPart(table);
        var idPart = ExtractStringPart(id);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="int"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, int id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        var tablePart = ExtractStringPart(table);
        var idPart = (value: id.ToString(), type: SpecialRecordPartType.None);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="long"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, long id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        var tablePart = ExtractStringPart(table);
        var idPart = (value: id.ToString(), type: SpecialRecordPartType.None);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="short"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, short id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        var tablePart = ExtractStringPart(table);
        var idPart = (value: id.ToString(), type: SpecialRecordPartType.None);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="byte"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, byte id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        var tablePart = ExtractStringPart(table);
        var idPart = (value: id.ToString(), type: SpecialRecordPartType.None);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and an <see cref="Guid"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, Guid id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        var tablePart = ExtractStringPart(table);
        var idPart = (value: CreateEscaped(id.ToString()), type: SpecialRecordPartType.None);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

    /// <summary>
    /// Creates a new record ID from a <see cref="string"/> typed table and a <see cref="ReadOnlyMemory{T}"/> typed id.
    /// </summary>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Thing From(string table, ReadOnlyMemory<byte> id)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table), "Table should not be null");

        var tablePart = ExtractStringPart(table);

        return new Thing(tablePart.value, tablePart.type, id);
    }
}
