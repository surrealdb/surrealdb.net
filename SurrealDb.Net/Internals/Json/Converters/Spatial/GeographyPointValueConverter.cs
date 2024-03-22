using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class GeographyPointValueConverter : JsonConverter<GeographyPoint>
{
    public override GeographyPoint? Read(
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
                        $"Cannot deserialize {nameof(GeographyPoint)} because coordinates must be an array"
                    );

                const int COORDINATES_ARRAY_LENGTH = 2;
                var arrayLength = coordinatesProperty.GetArrayLength();

                if (arrayLength != COORDINATES_ARRAY_LENGTH)
                    throw new JsonException(
                        $"Cannot deserialize {nameof(GeographyPoint)} because it contains wrong coordinates"
                    );

                var latitude = coordinatesProperty[1].GetDouble();
                var longitude = coordinatesProperty[0].GetDouble();

                return GeographyPoint.Create(latitude, longitude);
            }

            throw new JsonException(
                $"Cannot deserialize {nameof(GeographyPoint)} because of type \"{type}\""
            );
        }

        throw new JsonException($"Cannot deserialize {nameof(GeographyPoint)}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        GeographyPoint value,
        JsonSerializerOptions options
    )
    {
        PointConverter.WriteGeographyPoint(writer, value);
    }
}
