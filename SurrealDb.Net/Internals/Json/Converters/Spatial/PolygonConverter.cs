using System.Text.Json;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal static class PolygonConverter
{
    public const string TypeValue = "Polygon";

    public static void ConstructGeometryPolygon<T>(
        ref JsonElement coordinatesProperty,
        GeometryFactory<T> geometryBuilder
    )
        where T : Geometry
    {
        var pointsProperty = coordinatesProperty[0];

        foreach (var coordinate in pointsProperty.EnumerateArray())
        {
            var x = coordinate[0].GetDouble();
            var y = coordinate[1].GetDouble();

            geometryBuilder.LineTo(x, y);
        }
    }

    public static void ConstructGeographyPolygon<T>(
        ref JsonElement coordinatesProperty,
        GeographyFactory<T> geographyBuilder
    )
        where T : Geography
    {
        var pointsProperty = coordinatesProperty[0];

        foreach (var coordinate in pointsProperty.EnumerateArray())
        {
            var latitude = coordinate[1].GetDouble();
            var longitude = coordinate[0].GetDouble();

            geographyBuilder.LineTo(latitude, longitude);
        }
    }

    public static void WriteGeometryPolygon(Utf8JsonWriter writer, GeometryPolygon value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName(SpatialConverterConstants.TypePropertyName);
        writer.WriteStringValue(TypeValue);

        writer.WritePropertyName(SpatialConverterConstants.CoordinatesPropertyName);
        writer.WriteStartArray();

        foreach (var ring in value.Rings)
        {
            writer.WriteStartArray();

            foreach (var point in ring.Points)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(point.X);
                writer.WriteNumberValue(point.Y);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public static void WriteGeographyPolygon(Utf8JsonWriter writer, GeographyPolygon value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName(SpatialConverterConstants.TypePropertyName);
        writer.WriteStringValue(TypeValue);

        writer.WritePropertyName(SpatialConverterConstants.CoordinatesPropertyName);
        writer.WriteStartArray();

        foreach (var ring in value.Rings)
        {
            writer.WriteStartArray();

            foreach (var point in ring.Points)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(point.Longitude);
                writer.WriteNumberValue(point.Latitude);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
