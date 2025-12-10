using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RangeConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (
            string.Equals(type.Namespace, "SurrealDb.Net.Models", StringComparison.Ordinal)
            && string.Equals(type.Name, "Range`2", StringComparison.Ordinal)
        )
        {
            return CreateGenericConverter(
                options,
                typeof(RangeConverter<,>),
                type.GenericTypeArguments[0],
                type.GenericTypeArguments[1]
            );
        }

        return null;
    }
}
