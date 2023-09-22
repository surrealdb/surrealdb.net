#if NET6_0_OR_GREATER
using SurrealDb.Internals.Formatters;
using SurrealDb.Internals.Parsers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Internals.Json.Converters;

internal class TimeOnlyValueConverter : JsonConverter<TimeOnly>
{
	public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.TokenType switch
		{
			JsonTokenType.None or JsonTokenType.Null => default,
			JsonTokenType.String or JsonTokenType.PropertyName => GetValueFromString(reader.GetString()),
			JsonTokenType.Number => GetValueFromNumber(reader.GetInt64()),
			_ => throw new JsonException($"Cannot deserialize Duration to {nameof(TimeOnly)}")
		};

		return value;
	}

	private static TimeOnly GetValueFromNumber(long seconds)
	{
		var timeSpan = TimeSpan.FromSeconds(seconds);
		return TimeOnly.FromTimeSpan(timeSpan);
	}

	private static TimeOnly GetValueFromString(string? value)
	{
		if (value is null)
			return default;

		return TimeOnlyParser.Parse(value);
	}

	public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(TimeSpanFormatter.Format(value.ToTimeSpan()));
	}
}
#endif
