using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdRangeConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (
            type.Namespace == ConverterTypeConstants.ModelsNamespace
            && type.Name == "RecordIdRange`2"
        )
        {
            return CreateGenericConverter(
                options,
                typeof(RecordIdRangeConverter<,>),
                type.GenericTypeArguments[0],
                type.GenericTypeArguments[1]
            );
        }

        return null;
    }
}
