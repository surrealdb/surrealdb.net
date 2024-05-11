using System.Text.Json;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class LineStringConverter
{
    public const string TypeValue = "LineString";

    public static void ConstructGeometryLineString<T>(
        ref JsonElement coordinatesProperty,
        GeometryFactory<T> geometryBuilder
    )
        where T : Geometry
    {
        foreach (var coordinate in coordinatesProperty.EnumerateArray())
        {
            var x = coordinate[0].GetDouble();
            var y = coordinate[1].GetDouble();

            geometryBuilder.LineTo(x, y);
        }
    }

    public static void ConstructGeographyLineString<T>(
        ref JsonElement coordinatesProperty,
        GeographyFactory<T> geographyBuilder
    )
        where T : Geography
    {
        foreach (var coordinate in coordinatesProperty.EnumerateArray())
        {
            var latitude = coordinate[1].GetDouble();
            var longitude = coordinate[0].GetDouble();

            geographyBuilder.LineTo(latitude, longitude);
        }
    }

    public static void WriteGeometryLineString(Utf8JsonWriter writer, GeometryLineString value)
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

    public static void WriteGeographyLineString(Utf8JsonWriter writer, GeographyLineString value)
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
