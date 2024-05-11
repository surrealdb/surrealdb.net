using System.Text.Json;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class MultiLineStringConverter
{
    public const string TypeValue = "MultiLineString";

    public static void ConstructGeometryMultiLineString<T>(
        ref JsonElement coordinatesProperty,
        GeometryFactory<T> geometryBuilder
    )
        where T : Geometry
    {
        foreach (var lineProperty in coordinatesProperty.EnumerateArray())
        {
            geometryBuilder.LineString();

            foreach (var point in lineProperty.EnumerateArray())
            {
                var x = point[0].GetDouble();
                var y = point[1].GetDouble();

                geometryBuilder.LineTo(x, y);
            }
        }
    }

    public static void ConstructGeographyMultiLineString<T>(
        ref JsonElement coordinatesProperty,
        GeographyFactory<T> geographyBuilder
    )
        where T : Geography
    {
        foreach (var lineProperty in coordinatesProperty.EnumerateArray())
        {
            geographyBuilder.LineString();

            foreach (var point in lineProperty.EnumerateArray())
            {
                var latitude = point[1].GetDouble();
                var longitude = point[0].GetDouble();

                geographyBuilder.LineTo(latitude, longitude);
            }
        }
    }

    public static void WriteGeometryMultiLineString(
        Utf8JsonWriter writer,
        GeometryMultiLineString value
    )
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName(SpatialConverterConstants.TypePropertyName);
        writer.WriteStringValue(MultiLineStringConverter.TypeValue);

        writer.WritePropertyName(SpatialConverterConstants.CoordinatesPropertyName);
        writer.WriteStartArray();

        foreach (var line in value.LineStrings)
        {
            writer.WriteStartArray();

            foreach (var point in line.Points)
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

    public static void WriteGeographyMultiLineString(
        Utf8JsonWriter writer,
        GeographyMultiLineString value
    )
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName(SpatialConverterConstants.TypePropertyName);
        writer.WriteStringValue(MultiLineStringConverter.TypeValue);

        writer.WritePropertyName(SpatialConverterConstants.CoordinatesPropertyName);
        writer.WriteStartArray();

        foreach (var line in value.LineStrings)
        {
            writer.WriteStartArray();

            foreach (var point in line.Points)
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
