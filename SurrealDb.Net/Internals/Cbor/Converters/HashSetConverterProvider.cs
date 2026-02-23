using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class HashSetConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type is { Namespace: "System.Collections.Generic", Name: "HashSet`1" })
        {
            return CreateGenericConverter(
                options,
                typeof(HashSetConverter<>),
                type.GenericTypeArguments[0]
            );
        }

        return null;
    }
}
