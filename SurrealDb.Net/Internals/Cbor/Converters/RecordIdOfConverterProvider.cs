using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdOfConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (
            string.Equals(type.Namespace, "SurrealDb.Net.Models", StringComparison.Ordinal)
            && string.Equals(type.Name, "RecordIdOf`1", StringComparison.Ordinal)
        )
        {
            return CreateGenericConverter(
                options,
                typeof(RecordIdOfTConverter<>),
                type.GenericTypeArguments[0]
            );
        }

        return null;
    }
}
