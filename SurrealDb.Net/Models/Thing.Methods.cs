using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Models;
using System.Text;

namespace SurrealDb.Net.Models;

public partial class Thing
{
    /// <summary>
    /// Creates a new record ID from a genericly typed table and a genericly typed id.
    /// </summary>
    /// <typeparam name="TTable">The type of the table part</typeparam>
    /// <typeparam name="TId">The type of the record id part</typeparam>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
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

    private static (string value, SpecialRecordPartType type) ExtractThingPart<T>(T part)
    {
        if (part is string str)
        {
            bool shouldEscape = ShouldEscapeString(str);
            return (shouldEscape ? CreateEscaped(str) : str, SpecialRecordPartType.None);
        }

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

    private static bool ShouldEscapeString(string str)
    {
        if (long.TryParse(str, out _))
            return true;

        return !IsValidTextRecordId(str);
    }

    private static bool IsValidTextRecordId(string str)
    {
        return str.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static string CreateEscaped(string part)
    {
        var stringBuilder = new StringBuilder(part.Length + 2);
        stringBuilder.Append(ThingConstants.PREFIX);
        stringBuilder.Append(part);
        stringBuilder.Append(ThingConstants.SUFFIX);

        return stringBuilder.ToString();
    }

    internal string ToWsString()
    {
        if (
            _specialTableType == SpecialRecordPartType.None
            && _specialRecordIdType == SpecialRecordPartType.None
        )
        {
            return ToString();
        }

        var tablePart = ToWsPart(Table, _specialTableType);
        var idPart = ToWsPart(Id, _specialRecordIdType);

        var stringBuilder = new StringBuilder(tablePart.Length + 1 + idPart.Length);
        stringBuilder.Append(tablePart);
        stringBuilder.Append(ThingConstants.SEPARATOR);
        stringBuilder.Append(idPart);

        return stringBuilder.ToString();
    }

    private static string ToWsPart(string value, SpecialRecordPartType type)
    {
        switch (type)
        {
            case SpecialRecordPartType.JsonObject:
                return RewriteJsonObjectPart(value);
            case SpecialRecordPartType.JsonArray:
                return RewriteJsonArrayPart(value);
            default:
                return value;
        }
    }

    private static string RewriteJsonObjectPart(string part)
    {
        var jsonObject = JObject.Parse(part);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '\'';
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }

    private static string RewriteJsonArrayPart(string part)
    {
        var jsonObject = JArray.Parse(part);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '\'';
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }

    private static bool IsStringEscaped(ReadOnlySpan<char> span)
    {
        bool isDefaultEscaped =
            span[0] == ThingConstants.PREFIX && span[^1] == ThingConstants.SUFFIX;
        bool isAlternativeEscaped =
            span[0] == ThingConstants.ALTERNATE_ESCAPE
            && span[^1] == ThingConstants.ALTERNATE_ESCAPE;

        return isDefaultEscaped || isAlternativeEscaped;
    }
}
