using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyMultiLineStringConverter : CborConverterBase<GeographyMultiLineString>
{
    private readonly ICborConverter<GeographyLineString> _lineStringConverter;

    public GeographyMultiLineStringConverter(CborOptions options)
    {
        _lineStringConverter = options.Registry.ConverterRegistry.Lookup<GeographyLineString>();
    }

    public override GeographyMultiLineString Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geographyBuilder = GeographyFactory.MultiLineString();

        for (int i = 0; i < size; i++)
        {
            var line = _lineStringConverter.Read(ref reader);

            geographyBuilder.LineString();

            foreach (var point in line.Points)
            {
                geographyBuilder.LineTo(point.Latitude, point.Longitude);
            }
        }

        return geographyBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeographyMultiLineString value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_MULTILINE);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.LineStrings.Count);

        foreach (var line in value.LineStrings)
        {
            _lineStringConverter.Write(ref writer, line);
        }

        writer.WriteEndArray(value.LineStrings.Count);
    }
}
