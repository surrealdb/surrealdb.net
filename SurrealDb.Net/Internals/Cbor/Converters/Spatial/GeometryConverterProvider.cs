using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type == typeof(GeometryPoint))
        {
            return new GeometryPointConverter(options);
        }
        if (type == typeof(GeometryLineString))
        {
            return new GeometryLineStringConverter(options);
        }
        if (type == typeof(GeometryPolygon))
        {
            return new GeometryPolygonConverter(options);
        }
        if (type == typeof(GeometryMultiPoint))
        {
            return new GeometryMultiPointConverter(options);
        }
        if (type == typeof(GeometryMultiLineString))
        {
            return new GeometryMultiLineStringConverter(options);
        }
        if (type == typeof(GeometryMultiPolygon))
        {
            return new GeometryMultiPolygonConverter(options);
        }
        if (type == typeof(GeometryCollection))
        {
            return new GeometryCollectionConverter(options);
        }

        return null;
    }
}
