using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class TimeSpanValueConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.TokenType switch
		{
			JsonTokenType.None or JsonTokenType.Null => default,
			JsonTokenType.String or JsonTokenType.PropertyName => GetValueFromString(reader.GetString()),
			JsonTokenType.Number => GetValueFromNumber(reader.GetInt64()),
			_ => throw new JsonException($"Cannot deserialize Duration to {nameof(TimeSpan)}")
		};

		return value;
	}

	private static TimeSpan GetValueFromNumber(long seconds)
	{
		return TimeSpan.FromSeconds(seconds);
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
