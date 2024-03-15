using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Helpers;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class Vector3ValueConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
            return default;

        const byte VECTOR_LENGTH = 3;

        if (
            reader.TokenType == JsonTokenType.StartArray
            && VectorConverterHelper.TryReadVectorFromJsonArray(
                ref reader,
                VECTOR_LENGTH,
                out var values
            )
        )
#if NET6_0_OR_GREATER
            return new Vector3(values);
#else
            return new Vector3(values[0], values[1], values[2]);
#endif

        throw new JsonException($"Cannot deserialize {nameof(Vector3)}");
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);

        writer.WriteEndArray();
    }
}
