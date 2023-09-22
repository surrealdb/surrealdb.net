using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Models;

namespace SurrealDb.Internals.Json.Converters;

internal class DurationConverter : JsonConverter<Duration>
{
	public override Duration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.TokenType switch
		{
			JsonTokenType.None or JsonTokenType.Null => default,
			JsonTokenType.String or JsonTokenType.PropertyName => GetValueFromString(reader.GetString()),
			_ => throw new JsonException("Cannot deserialize Duration")
		};

		return value;
	}

	private static Duration GetValueFromString(string? value)
	{
		if (value is null)
			return default;

		return new Duration(value);
	}

	public override void Write(Utf8JsonWriter writer, Duration value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
