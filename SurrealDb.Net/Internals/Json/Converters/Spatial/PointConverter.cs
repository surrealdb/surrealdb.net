using System.Text.Json;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class PointConverter
{
    public const string TypeValue = "Point";

    public static void ConstructGeometryPoint<T>(
        ref JsonElement coordinatesProperty,
        GeometryFactory<T> geometryBuilder
    )
        where T : Geometry
    {
        var x = coordinatesProperty[0].GetDouble();
        var y = coordinatesProperty[1].GetDouble();

        geometryBuilder.Point(x, y);
    }

    public static void ConstructGeographyPoint<T>(
        ref JsonElement coordinatesProperty,
        GeographyFactory<T> geographyBuilder
    )
        where T : Geography
    {
        var latitude = coordinatesProperty[1].GetDouble();
        var longitude = coordinatesProperty[0].GetDouble();

        geographyBuilder.Point(latitude, longitude);
    }

    public static void WriteGeometryPoint(Utf8JsonWriter writer, GeometryPoint value)
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
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public static void WriteGeographyPoint(Utf8JsonWriter writer, GeographyPoint value)
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
        writer.WriteNumberValue(value.Longitude);
        writer.WriteNumberValue(value.Latitude);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
