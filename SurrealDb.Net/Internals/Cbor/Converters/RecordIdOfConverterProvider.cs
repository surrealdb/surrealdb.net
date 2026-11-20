using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdOfConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type.Namespace == ConverterTypeConstants.ModelsNamespace && type.Name == "RecordIdOf`1")
        {
            // The record id types read from the server are constructed statically,
            // so that they never reach MakeGenericType (not supported by Native AOT).
            if (type == typeof(RecordIdOf<int>))
            {
                return new RecordIdOfTConverter<int>(options);
            }
            if (type == typeof(RecordIdOf<long>))
            {
                return new RecordIdOfTConverter<long>(options);
            }
            if (type == typeof(RecordIdOf<Guid>))
            {
                return new RecordIdOfTConverter<Guid>(options);
            }
            if (type == typeof(RecordIdOf<string>))
            {
                return new RecordIdOfTConverter<string>(options);
            }

            return CreateGenericConverter(
                options,
                typeof(RecordIdOfTConverter<>),
                type.GenericTypeArguments[0]
            );
        }

        return null;
    }
}
