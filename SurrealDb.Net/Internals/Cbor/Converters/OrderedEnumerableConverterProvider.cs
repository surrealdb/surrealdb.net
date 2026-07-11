using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class OrderedEnumerableConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>))
        {
            return CreateGenericConverter(
                options,
                typeof(OrderedEnumerableConverter<>),
                type.GenericTypeArguments[0]
            );
        }

        return null;
    }
}
