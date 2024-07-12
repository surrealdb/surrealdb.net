using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyPolygonConverter : CborConverterBase<GeographyPolygon>
{
    private readonly ICborConverter<GeographyLineString> _lineStringConverter;

    public GeographyPolygonConverter(CborOptions options)
    {
        _lineStringConverter = options.Registry.ConverterRegistry.Lookup<GeographyLineString>();
    }

    public override GeographyPolygon Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geographyBuilder = GeographyFactory.Polygon();

        for (int i = 0; i < size; i++)
        {
            var line = _lineStringConverter.Read(ref reader);

            var firstPoint = line.Points.First();
            geographyBuilder.Ring(firstPoint.Latitude, firstPoint.Longitude);

            foreach (var point in line.Points.Skip(1))
            {
                geographyBuilder.LineTo(point.Latitude, point.Longitude);
            }
        }

        return geographyBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeographyPolygon value)
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
