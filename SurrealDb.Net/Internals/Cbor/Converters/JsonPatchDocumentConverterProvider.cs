using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class JsonPatchDocumentConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type.Namespace != "SystemTextJsonPatch")
        {
            return null;
        }

        return type switch
        {
            { Name: "JsonPatchDocument", IsGenericType: false } => new JsonPatchDocumentConverter(
                options
            ),
            { Name: "JsonPatchDocument`1", IsGenericType: true } => CreateGenericConverter(
                options,
                typeof(JsonPatchDocumentConverter<>),
                type.GenericTypeArguments[0]
            ),
            _ => null,
        };
    }
}
