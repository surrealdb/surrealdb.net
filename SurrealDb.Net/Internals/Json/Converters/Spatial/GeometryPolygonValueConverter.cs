using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class GeometryPolygonValueConverter : JsonConverter<GeometryPolygon>
{
    public override GeometryPolygon? Read(
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

            if (type == PolygonConverter.TypeValue)
            {
                var coordinatesProperty = root.GetProperty(
                    SpatialConverterConstants.CoordinatesPropertyName
                );

                if (coordinatesProperty.ValueKind != JsonValueKind.Array)
                    throw new JsonException(
                        $"Cannot deserialize {nameof(GeometryPolygon)} because coordinates must be an array"
                    );

                var geometryBuilder = GeometryFactory.Polygon();

                PolygonConverter.ConstructGeometryPolygon(ref coordinatesProperty, geometryBuilder);

                return geometryBuilder.Build();
            }

            throw new JsonException(
                $"Cannot deserialize {nameof(GeometryPolygon)} because of type \"{type}\""
            );
        }

        throw new JsonException($"Cannot deserialize {nameof(GeometryPolygon)}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        GeometryPolygon value,
        JsonSerializerOptions options
    )
    {
        PolygonConverter.WriteGeometryPolygon(writer, value);
    }
}
