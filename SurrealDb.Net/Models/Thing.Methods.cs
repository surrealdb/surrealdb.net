using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Models;

public partial class Thing
{
    private static (string value, SpecialRecordPartType type) ExtractStringPart(string part)
    {
        bool shouldEscape = ShouldEscapeString(part);
        return (shouldEscape ? CreateEscaped(part) : part, SpecialRecordPartType.None);
    }

    internal static bool ShouldEscapeString(string str)
    {
        if (long.TryParse(str, out _))
        {
            return true;
        }

        return !IsValidTextRecordId(str);
    }

    private static bool IsValidTextRecordId(string str)
    {
        foreach (char c in str)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    internal static string CreateEscaped(string part)
    {
        var stringBuilder = new StringBuilder(part.Length + 2);
        stringBuilder.Append(ThingConstants.PREFIX);
        stringBuilder.Append(part.AsSpan());
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
