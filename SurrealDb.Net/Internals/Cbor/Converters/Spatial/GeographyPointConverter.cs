using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyPointConverter : CborConverterBase<GeographyPoint>
{
    private readonly ICborConverter<decimal> _decimalConverter;

    public GeographyPointConverter(CborOptions options)
    {
        _decimalConverter = options.Registry.ConverterRegistry.Lookup<decimal>();
    }

    public override GeographyPoint Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        var longitude = _decimalConverter.Read(ref reader);
        var latitude = _decimalConverter.Read(ref reader);

        return GeographyPoint.Create((double)latitude, (double)longitude);
    }

    public override void Write(ref CborWriter writer, GeographyPoint value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_POINT);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteString(value.Longitude.ToString());
        writer.WriteString(value.Latitude.ToString());

        writer.WriteEndArray(2);
    }
}
