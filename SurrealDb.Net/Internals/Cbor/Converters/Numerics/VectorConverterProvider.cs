using System.Numerics;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters.Numerics;

internal class VectorConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type == typeof(Vector2))
        {
            return new Vector2Converter();
        }
        if (type == typeof(Vector3))
        {
            return new Vector3Converter();
        }
        if (type == typeof(Vector4))
        {
            return new Vector4Converter();
        }

        return null;
    }
}
