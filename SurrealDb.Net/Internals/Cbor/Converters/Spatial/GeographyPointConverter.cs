using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyPointConverter : CborConverterBase<GeographyPoint>
{
    private readonly ICborConverter<double> _doubleConverter;

    public GeographyPointConverter(CborOptions options)
    {
        _doubleConverter = options.Registry.ConverterRegistry.Lookup<double>();
    }

    public override GeographyPoint Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        var longitude = _doubleConverter.Read(ref reader);
        var latitude = _doubleConverter.Read(ref reader);

        return GeographyPoint.Create(latitude, longitude);
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

        writer.WriteDouble(value.Longitude);
        writer.WriteDouble(value.Latitude);

        writer.WriteEndArray(2);
    }
}
