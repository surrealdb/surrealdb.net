using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryMultiPolygonConverter : CborConverterBase<GeometryMultiPolygon>
{
    private readonly ICborConverter<GeometryPolygon> _polygonConverter;

    public GeometryMultiPolygonConverter(CborOptions options)
    {
        _polygonConverter = options.Registry.ConverterRegistry.Lookup<GeometryPolygon>();
    }

    public override GeometryMultiPolygon Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geometryBuilder = GeometryFactory.MultiPolygon();

        for (int i = 0; i < size; i++)
        {
            var polygon = _polygonConverter.Read(ref reader);

            geometryBuilder.Polygon();

            foreach (var lineString in polygon.Rings)
            {
                var firstPoint = lineString.Points.First();
                geometryBuilder.Ring(firstPoint.X, firstPoint.Y);

                foreach (var point in lineString.Points.Skip(1))
                {
                    geometryBuilder.LineTo(point.X, point.Y);
                }
            }
        }

        return geometryBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeometryMultiPolygon value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_MULTIPOLYGON);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.Polygons.Count);

        foreach (var polygon in value.Polygons)
        {
            _polygonConverter.Write(ref writer, polygon);
        }

        writer.WriteEndArray(value.Polygons.Count);
    }
}
