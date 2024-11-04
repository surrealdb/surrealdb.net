using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdOfConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type.Namespace == "SurrealDb.Net.Models" && type.Name == "RecordIdOf`1")
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
