using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Internals.Formatters;
using SurrealDb.Internals.Parsers;

namespace SurrealDb.Internals.Json.Converters;

internal class TimeSpanValueConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.TokenType switch
		{
			JsonTokenType.None or JsonTokenType.Null => default,
			JsonTokenType.String or JsonTokenType.PropertyName => GetValueFromString(reader.GetString()),
			JsonTokenType.Number => TimeSpan.FromTicks(reader.GetInt64()),
			_ => throw new JsonException("Cannot deserialize Duration to TimeSpan")
		};

		return value;
	}

	private static TimeSpan GetValueFromString(string? value)
	{
		if (value is null)
			return default;

		return TimeSpanParser.Parse(value);
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(TimeSpanFormatter.Format(value));
    }
}
