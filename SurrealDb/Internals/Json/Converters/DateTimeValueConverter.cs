using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Internals.Parsers;

namespace SurrealDb.Internals.Json.Converters;

internal class DateTimeValueConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.TokenType switch
        {
            JsonTokenType.None or JsonTokenType.Null => default,
            JsonTokenType.String or JsonTokenType.PropertyName => GetValueFromString(reader.GetString()),
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()).UtcDateTime,
            _ => throw new JsonException($"Cannot deserialize Date to {nameof(DateTime)}")
        };

        return value;
	}

	private static DateTime GetValueFromString(string? value)
	{
		if (value is null)
			return default;

		return DateTimeParser.Parse(value);
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O"));
    }
}
