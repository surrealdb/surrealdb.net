using System.Diagnostics.CodeAnalysis;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Models;

public partial class Thing
{
    /// <summary>
    /// Creates a new record ID from a generically typed table and a generically typed id.
    /// </summary>
    /// <typeparam name="TTable">The type of the table part</typeparam>
    /// <typeparam name="TId">The type of the record id part</typeparam>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(
        "Requires reflection for JSON serialization of potential objects/arrays record id"
    )]
    [RequiresUnreferencedCode(
        "Requires reflection for JSON serialization of potential objects/arrays record id"
    )]
#endif
    public static Thing From<TTable, TId>(TTable table, TId id)
    {
        if (table is null)
            throw new ArgumentException("Table should not be null", nameof(table));

        if (id is null)
            throw new ArgumentException("Id should not be null", nameof(id));

        var tablePart = ExtractThingPart(table);
        var idPart = ExtractThingPart(id);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Requires reflection for JSON serialization of objects/arrays part")]
    [RequiresUnreferencedCode("Requires reflection for JSON serialization of objects/arrays part")]
#endif
    private static (string value, SpecialRecordPartType type) ExtractThingPart<T>(T part)
    {
        if (part is string str)
            return ExtractStringPart(str);

        if (part is int i)
            return (i.ToString(), SpecialRecordPartType.None);

        if (part is long l)
            return (l.ToString(), SpecialRecordPartType.None);

        if (part is short s)
            return (s.ToString(), SpecialRecordPartType.None);

        if (part is byte b)
            return (b.ToString(), SpecialRecordPartType.None);

        if (part is char c)
            return (c.ToString(), SpecialRecordPartType.None);

        if (part is Guid guid)
            return (CreateEscaped(guid.ToString()), SpecialRecordPartType.None);

        if (part is sbyte sb)
            return (sb.ToString(), SpecialRecordPartType.None);

        if (part is ushort us)
            return (us.ToString(), SpecialRecordPartType.None);

        if (part is uint ui)
            return (ui.ToString(), SpecialRecordPartType.None);

        if (part is ulong ul)
            return (ul.ToString(), SpecialRecordPartType.None);

        var serializedPart = System.Text.Json.JsonSerializer.Serialize(
            part,
            SurrealDbSerializerOptions.Default
        );

        char start = serializedPart[0];
        char end = serializedPart[^1];

        bool isJsonObject = start == '{' && end == '}';
        if (isJsonObject)
            return (serializedPart, SpecialRecordPartType.JsonObject);

        bool isJsonArray = start == '[' && end == ']';
        if (isJsonArray)
            return (serializedPart, SpecialRecordPartType.JsonArray);

        throw new NotImplementedException();
    }
}
