using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryMultiLineStringConverter : CborConverterBase<GeometryMultiLineString>
{
    private readonly ICborConverter<GeometryLineString> _lineStringConverter;

    public GeometryMultiLineStringConverter(CborOptions options)
    {
        _lineStringConverter = options.Registry.ConverterRegistry.Lookup<GeometryLineString>();
    }

    public override GeometryMultiLineString Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geometryBuilder = GeometryFactory.MultiLineString();

        for (int i = 0; i < size; i++)
        {
            var line = _lineStringConverter.Read(ref reader);

            geometryBuilder.LineString();

            foreach (var point in line.Points)
            {
                geometryBuilder.LineTo(point.X, point.Y);
            }
        }

        return geometryBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeometryMultiLineString value)
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
