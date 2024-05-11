using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryPointConverter : CborConverterBase<GeometryPoint>
{
    private readonly ICborConverter<decimal> _decimalConverter;

    public GeometryPointConverter(CborOptions options)
    {
        _decimalConverter = options.Registry.ConverterRegistry.Lookup<decimal>();
    }

    public override GeometryPoint Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        var x = _decimalConverter.Read(ref reader);
        var y = _decimalConverter.Read(ref reader);

        return GeometryPoint.Create((double)x, (double)y);
    }

    public override void Write(ref CborWriter writer, GeometryPoint value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_POINT);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteString(value.X.ToString());
        writer.WriteString(value.Y.ToString());

        writer.WriteEndArray(2);
    }
}
