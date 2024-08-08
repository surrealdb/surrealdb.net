using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyMultiPolygonConverter : CborConverterBase<GeographyMultiPolygon>
{
    private readonly ICborConverter<GeographyPolygon> _polygonConverter;

    public GeographyMultiPolygonConverter(CborOptions options)
    {
        _polygonConverter = options.Registry.ConverterRegistry.Lookup<GeographyPolygon>();
    }

    public override GeographyMultiPolygon Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geographyBuilder = GeographyFactory.MultiPolygon();

        for (int i = 0; i < size; i++)
        {
            var polygon = _polygonConverter.Read(ref reader);

            geographyBuilder.Polygon();

            foreach (var lineString in polygon.Rings)
            {
                var firstPoint = lineString.Points.First();
                geographyBuilder.Ring(firstPoint.Latitude, firstPoint.Longitude);

                foreach (var point in lineString.Points.Skip(1))
                {
                    geographyBuilder.LineTo(point.Latitude, point.Longitude);
                }
            }
        }

        return geographyBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeographyMultiPolygon value)
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
