using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
    /// <param name="jsonNamingPolicy">The naming policy to use when serializing the record id</param>
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
    public static Thing From<TTable, TId>(
        TTable table,
        TId id,
        JsonNamingPolicy? jsonNamingPolicy = null
    )
    {
        if (table is null)
            throw new ArgumentException("Table should not be null", nameof(table));

        if (id is null)
            throw new ArgumentException("Id should not be null", nameof(id));

        var tablePart = ExtractThingPart(table, jsonNamingPolicy);
        var idPart = ExtractThingPart(id, jsonNamingPolicy);

        return new Thing(tablePart.value, tablePart.type, idPart.value, idPart.type);
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Requires reflection for JSON serialization of objects/arrays part")]
    [RequiresUnreferencedCode("Requires reflection for JSON serialization of objects/arrays part")]
#endif
    private static (string value, SpecialRecordPartType type) ExtractThingPart<T>(
        T part,
        JsonNamingPolicy? jsonNamingPolicy
    )
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

        var serializedPart = JsonSerializer.Serialize(
            part,
            SurrealDbSerializerOptions.GetDefaultSerializerFromPolicy(jsonNamingPolicy)
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
