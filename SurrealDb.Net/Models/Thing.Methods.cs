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
    /// Creates a new record ID from a table and a genericly typed id.
    /// </summary>
    /// <typeparam name="T">The type of the table id</typeparam>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static Thing From<T>(ReadOnlySpan<char> table, T id)
    {
        if (id is null)
            throw new ArgumentException("Id should not be null", nameof(id));

        if (id is string str)
        {
            bool shouldEscape = ShouldEscapeString(str);
            string recordId = shouldEscape ? CreateEscapedId(str) : str;

            return new(table, recordId);
        }

        if (id is int i)
            return new(table, i.ToString());

        if (id is long l)
            return new(table, l.ToString());

        if (id is short s)
            return new(table, s.ToString());

        if (id is byte b)
            return new(table, b.ToString());

        if (id is char c)
            return new(table, c.ToString());

        if (id is Guid guid)
            return new(table, CreateEscapedId(guid.ToString()));

        if (id is sbyte sb)
            return new(table, sb.ToString());

        if (id is ushort us)
            return new(table, us.ToString());

        if (id is uint ui)
            return new(table, ui.ToString());

        if (id is ulong ul)
            return new(table, ul.ToString());

        var serializedId = System.Text.Json.JsonSerializer.Serialize(
            id,
            SurrealDbSerializerOptions.Default
        );

        char start = serializedId[0];
        char end = serializedId[^1];

        bool isJsonObject = start == '{' && end == '}';
        if (isJsonObject)
            return new(table, serializedId, SpecialRecordIdType.JsonObject);

        bool isJsonArray = start == '[' && end == ']';
        if (isJsonArray)
            return new(table, serializedId, SpecialRecordIdType.JsonArray);

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

    private static string CreateEscapedId(string id)
    {
        var stringBuilder = new StringBuilder(id.Length + 2);
        stringBuilder.Append(ThingConstants.PREFIX);
        stringBuilder.Append(id);
        stringBuilder.Append(ThingConstants.SUFFIX);

        return stringBuilder.ToString();
    }

    internal string ToWsString()
    {
        if (_specialRecordIdType == SpecialRecordIdType.JsonObject)
        {
            string rewrittenId = RewriteJsonObjectId(Id);

            var stringBuilder = new StringBuilder(TableSpan.Length + 1 + rewrittenId.Length);
            stringBuilder.Append(TableSpan);
            stringBuilder.Append(ThingConstants.SEPARATOR);
            stringBuilder.Append(rewrittenId);

            return stringBuilder.ToString();
        }

        if (_specialRecordIdType == SpecialRecordIdType.JsonArray)
        {
            string rewrittenId = RewriteJsonArrayId(Id);

            var stringBuilder = new StringBuilder(TableSpan.Length + 1 + rewrittenId.Length);
            stringBuilder.Append(TableSpan);
            stringBuilder.Append(ThingConstants.SEPARATOR);
            stringBuilder.Append(rewrittenId);

            return stringBuilder.ToString();
        }

        return ToString();
    }

    private static string RewriteJsonObjectId(string id)
    {
        var jsonObject = JObject.Parse(id);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '\'';
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }

    private static string RewriteJsonArrayId(string id)
    {
        var jsonObject = JArray.Parse(id);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '\'';
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }
}
