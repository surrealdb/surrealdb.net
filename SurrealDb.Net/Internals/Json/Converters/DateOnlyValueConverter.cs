#if NET6_0_OR_GREATER
using SurrealDb.Net.Internals.Parsers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class DateOnlyValueConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var value = reader.TokenType switch
        {
            JsonTokenType.None or JsonTokenType.Null => default,
            JsonTokenType.String
            or JsonTokenType.PropertyName
                => GetValueFromString(reader.GetString()),
            JsonTokenType.Number => GetValueFromNumber(reader.GetInt64()),
            _ => throw new JsonException($"Cannot deserialize Date to {nameof(DateOnly)}")
        };

        return value;
    }

    private static DateOnly GetValueFromString(string? value)
    {
        if (value is null)
            return default;

        return DateOnlyParser.Parse(value);
    }

    private static DateOnly GetValueFromNumber(long number)
    {
        var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(number).UtcDateTime;
        return new DateOnly(utcDateTime.Year, utcDateTime.Month, utcDateTime.Day);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToDateTime(new(), DateTimeKind.Utc).ToString("O"));
    }
}
#endif
