using System.Text;
using Newtonsoft.Json.Linq;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Models;

public partial class RecordId
{
    public T? DeserializeId<T>()
    {
        if (_specialRecordIdType == SpecialRecordPartType.None)
        {
            throw new NotSupportedException(
                $"This id is not serialized. Please use the {nameof(Id)} property."
            );
        }

        if (_specialRecordIdType == SpecialRecordPartType.SerializedCbor)
        {
            // TODO : Pass down CborOptions
            return CborSerializer.Deserialize<T>(
                _serializedCborId!.Value.Span,
                SurrealDbCborOptions.Default
            );
        }

        throw new NotSupportedException("JSON deserialization is no longer supported.");
    }

    private static string ReconstructJsonFromObject(ReadOnlySpan<char> part)
    {
        var jsonObject = JObject.Parse(part.ToString());
        return ReconstructJson(jsonObject);
    }

    private static string ReconstructJsonFromArray(ReadOnlySpan<char> part)
    {
        var jsonObject = JArray.Parse(part.ToString());
        return ReconstructJson(jsonObject);
    }

    private static string ReconstructJson(JContainer jsonObject)
    {
        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new Newtonsoft.Json.JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '"';
        jsonTextWriter.QuoteName = true;
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }

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
        stringBuilder.Append(RecordIdConstants.PREFIX);
        stringBuilder.Append(part.AsSpan());
        stringBuilder.Append(RecordIdConstants.SUFFIX);

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

        var tablePart = ToWsPart(TableSpan, _specialTableType);
        var idPart = ToWsPart(IdSpan, _specialRecordIdType);

        var stringBuilder = new StringBuilder(tablePart.Length + 1 + idPart.Length);
        stringBuilder.Append(tablePart);
        stringBuilder.Append(RecordIdConstants.SEPARATOR);
        stringBuilder.Append(idPart);

        return stringBuilder.ToString();
    }

    private static string ToWsPart(ReadOnlySpan<char> value, SpecialRecordPartType type)
    {
        return type switch
        {
            SpecialRecordPartType.JsonObject => RewriteJsonObjectPart(value),
            SpecialRecordPartType.JsonArray => RewriteJsonArrayPart(value),
            _ => value.ToString()
        };
    }

    private static string RewriteJsonObjectPart(ReadOnlySpan<char> part)
    {
        var jsonObject = JObject.Parse(part.ToString());

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new Newtonsoft.Json.JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '\'';
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }

    private static string RewriteJsonArrayPart(ReadOnlySpan<char> part)
    {
        var jsonObject = JArray.Parse(part.ToString());

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        using var jsonTextWriter = new Newtonsoft.Json.JsonTextWriter(writer);

        jsonTextWriter.QuoteChar = '\'';
        jsonObject.WriteTo(jsonTextWriter);

        return stringBuilder.ToString();
    }

    private static bool IsStringEscaped(ReadOnlySpan<char> span)
    {
        bool isDefaultEscaped =
            span[0] == RecordIdConstants.PREFIX && span[^1] == RecordIdConstants.SUFFIX;
        bool isAlternativeEscaped =
            span[0] == RecordIdConstants.ALTERNATE_ESCAPE
            && span[^1] == RecordIdConstants.ALTERNATE_ESCAPE;

        return isDefaultEscaped || isAlternativeEscaped;
    }
}
