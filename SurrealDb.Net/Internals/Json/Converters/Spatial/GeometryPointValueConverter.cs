using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class GeometryPointValueConverter : JsonConverter<GeometryPoint>
{
    public override GeometryPoint? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
            return default;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty(SpatialConverterConstants.TypePropertyName, out var typeProperty))
        {
            var type = typeProperty.GetString();

            if (type == PointConverter.TypeValue)
            {
                var coordinatesProperty = root.GetProperty(
                    SpatialConverterConstants.CoordinatesPropertyName
                );

                if (coordinatesProperty.ValueKind != JsonValueKind.Array)
                    throw new JsonException(
                        $"Cannot deserialize {nameof(GeometryPoint)} because coordinates must be an array"
                    );

                const int COORDINATES_ARRAY_LENGTH = 2;
                var arrayLength = coordinatesProperty.GetArrayLength();

                if (arrayLength != COORDINATES_ARRAY_LENGTH)
                    throw new JsonException(
                        $"Cannot deserialize {nameof(GeometryPoint)} because it contains wrong coordinates"
                    );

                var x = coordinatesProperty[0].GetDouble();
                var y = coordinatesProperty[1].GetDouble();

                return GeometryPoint.Create(x, y);
            }

            throw new JsonException(
                $"Cannot deserialize {nameof(GeometryPoint)} because of type \"{type}\""
            );
        }

        throw new JsonException($"Cannot deserialize {nameof(GeometryPoint)}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        GeometryPoint value,
        JsonSerializerOptions options
    )
    {
        PointConverter.WriteGeometryPoint(writer, value);
    }
}
