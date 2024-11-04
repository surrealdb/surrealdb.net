using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RangeBoundConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type.Namespace == "SurrealDb.Net.Models" && type.Name == "RangeBound`1")
        {
            return CreateGenericConverter(
                options,
                typeof(RangeBoundConverter<>),
                type.GenericTypeArguments[0]
            );
        }

        return null;
    }
}
