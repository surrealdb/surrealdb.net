using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryLineStringConverter : CborConverterBase<GeometryLineString>
{
    private readonly ICborConverter<GeometryPoint> _pointConverter;

    public GeometryLineStringConverter(CborOptions options)
    {
        _pointConverter = options.Registry.ConverterRegistry.Lookup<GeometryPoint>();
    }

    public override GeometryLineString Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geometryBuilder = GeometryFactory.LineString();

        for (int i = 0; i < size; i++)
        {
            var point = _pointConverter.Read(ref reader);
            geometryBuilder.LineTo(point.X, point.Y);
        }

        return geometryBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeometryLineString value)
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
