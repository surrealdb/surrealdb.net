using SurrealDb.Internals.Helpers;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Internals.Json.Converters;

internal class Vector2ValueConverter : JsonConverter<Vector2>
{
	public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
			return default;

		const byte VECTOR_LENGTH = 2;

		if (reader.TokenType == JsonTokenType.StartArray &&
			VectorConverterHelper.TryReadVectorFromJsonArray(ref reader, VECTOR_LENGTH, out var values)
		)
#if NET6_0_OR_GREATER
			return new Vector2(values);
#else
			return new Vector2(values[0], values[1]);
#endif

		throw new JsonException($"Cannot deserialize {nameof(Vector2)}");
	}

	public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();

		writer.WriteNumberValue(value.X);
		writer.WriteNumberValue(value.Y);

		writer.WriteEndArray();
	}
}
