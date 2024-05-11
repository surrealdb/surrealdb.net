using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyLineStringConverter : CborConverterBase<GeographyLineString>
{
    private readonly ICborConverter<GeographyPoint> _pointConverter;

    public GeographyLineStringConverter(CborOptions options)
    {
        _pointConverter = options.Registry.ConverterRegistry.Lookup<GeographyPoint>();
    }

    public override GeographyLineString Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geographyBuilder = GeographyFactory.LineString();

        for (int i = 0; i < size; i++)
        {
            var point = _pointConverter.Read(ref reader);
            geographyBuilder.LineTo(point.Latitude, point.Longitude);
        }

        return geographyBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeographyLineString value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_LINE);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.Points.Count);

        foreach (var point in value.Points)
        {
            _pointConverter.Write(ref writer, point);
        }

        writer.WriteEndArray(value.Points.Count);
    }
}
