using System.Text.Json;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal static class MultiPointConverter
{
    public const string TypeValue = "MultiPoint";

    public static void ConstructGeometryMultiPoint<T>(
        ref JsonElement coordinatesProperty,
        GeometryFactory<T> geometryBuilder
    )
        where T : Geometry
    {
        foreach (var point in coordinatesProperty.EnumerateArray())
        {
            var x = point[0].GetDouble();
            var y = point[1].GetDouble();

            geometryBuilder.Point(x, y);
        }
    }

    public static void ConstructGeographyMultiPoint<T>(
        ref JsonElement coordinatesProperty,
        GeographyFactory<T> geographyBuilder
    )
        where T : Geography
    {
        foreach (var point in coordinatesProperty.EnumerateArray())
        {
            var latitude = point[1].GetDouble();
            var longitude = point[0].GetDouble();

            geographyBuilder.Point(latitude, longitude);
        }
    }

    public static void WriteGeometryMultiPoint(Utf8JsonWriter writer, GeometryMultiPoint value)
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

        foreach (var point in value.Points)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(point.X);
            writer.WriteNumberValue(point.Y);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public static void WriteGeographyMultiPoint(Utf8JsonWriter writer, GeographyMultiPoint value)
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

        foreach (var point in value.Points)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(point.Longitude);
            writer.WriteNumberValue(point.Latitude);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
