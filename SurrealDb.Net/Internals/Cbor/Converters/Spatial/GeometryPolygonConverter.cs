using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryPolygonConverter : CborConverterBase<GeometryPolygon>
{
    private readonly ICborConverter<GeometryLineString> _lineStringConverter;

    public GeometryPolygonConverter(CborOptions options)
    {
        _lineStringConverter = options.Registry.ConverterRegistry.Lookup<GeometryLineString>();
    }

    public override GeometryPolygon Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geometryBuilder = GeometryFactory.Polygon();

        for (int i = 0; i < size; i++)
        {
            var line = _lineStringConverter.Read(ref reader);

            var firstPoint = line.Points.First();
            geometryBuilder.Ring(firstPoint.X, firstPoint.Y);

            foreach (var point in line.Points.Skip(1))
            {
                geometryBuilder.LineTo(point.X, point.Y);
            }
        }

        return geometryBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeometryPolygon value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_POLYGON);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.Rings.Count);

        foreach (var ring in value.Rings)
        {
            _lineStringConverter.Write(ref writer, ring);
        }

        writer.WriteEndArray(value.Rings.Count);
    }
}
