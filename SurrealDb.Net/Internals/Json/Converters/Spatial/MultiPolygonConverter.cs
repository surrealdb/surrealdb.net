using System.Text.Json;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal static class MultiPolygonConverter
{
    public const string TypeValue = "MultiPolygon";

    public static void ConstructGeometryMultiPolygon<T>(
        ref JsonElement coordinatesProperty,
        GeometryFactory<T> geometryBuilder
    )
        where T : Geometry
    {
        foreach (var polygonProperty in coordinatesProperty.EnumerateArray())
        {
            geometryBuilder.Polygon();

            var pointsProperty = polygonProperty[0];

            foreach (var point in pointsProperty.EnumerateArray())
            {
                var x = point[0].GetDouble();
                var y = point[1].GetDouble();

                geometryBuilder.LineTo(x, y);
            }
        }
    }

    public static void ConstructGeographyMultiPolygon<T>(
        ref JsonElement coordinatesProperty,
        GeographyFactory<T> geographyBuilder
    )
        where T : Geography
    {
        foreach (var polygonProperty in coordinatesProperty.EnumerateArray())
        {
            geographyBuilder.Polygon();

            var pointsProperty = polygonProperty[0];

            foreach (var point in pointsProperty.EnumerateArray())
            {
                var latitude = point[1].GetDouble();
                var longitude = point[0].GetDouble();

                geographyBuilder.LineTo(latitude, longitude);
            }
        }
    }

    public static void WriteGeometryMultiPolygon(Utf8JsonWriter writer, GeometryMultiPolygon value)
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

        foreach (var polygon in value.Polygons)
        {
            writer.WriteStartArray();

            foreach (var ring in polygon.Rings)
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
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public static void WriteGeographyMultiPolygon(
        Utf8JsonWriter writer,
        GeographyMultiPolygon value
    )
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

        foreach (var polygon in value.Polygons)
        {
            writer.WriteStartArray();

            foreach (var ring in polygon.Rings)
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
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
