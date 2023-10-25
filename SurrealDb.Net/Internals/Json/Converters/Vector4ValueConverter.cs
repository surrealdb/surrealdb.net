using SurrealDb.Net.Internals.Helpers;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class Vector4ValueConverter : JsonConverter<Vector4>
{
    public override Vector4 Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
            return default;

        const byte VECTOR_LENGTH = 4;

        if (
            reader.TokenType == JsonTokenType.StartArray
            && VectorConverterHelper.TryReadVectorFromJsonArray(
                ref reader,
                VECTOR_LENGTH,
                out var values
            )
        )
#if NET6_0_OR_GREATER
            return new Vector4(values);
#else
            return new Vector4(values[0], values[1], values[2], values[3]);
#endif

        throw new JsonException($"Cannot deserialize {nameof(Vector4)}");
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);

        writer.WriteEndArray();
    }
}
