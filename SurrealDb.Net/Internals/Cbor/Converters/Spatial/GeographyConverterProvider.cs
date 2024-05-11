using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type == typeof(GeographyPoint))
        {
            return new GeographyPointConverter(options);
        }
        if (type == typeof(GeographyLineString))
        {
            return new GeographyLineStringConverter(options);
        }
        if (type == typeof(GeographyPolygon))
        {
            return new GeographyPolygonConverter(options);
        }
        if (type == typeof(GeographyMultiPoint))
        {
            return new GeographyMultiPointConverter(options);
        }
        if (type == typeof(GeographyMultiLineString))
        {
            return new GeographyMultiLineStringConverter(options);
        }
        if (type == typeof(GeographyMultiPolygon))
        {
            return new GeographyMultiPolygonConverter(options);
        }
        if (type == typeof(GeographyCollection))
        {
            return new GeographyCollectionConverter(options);
        }

        return null;
    }
}
