using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class NoneConverter : CborConverterBase<None>
{
    public override None Read(ref CborReader reader)
    {
        if (
            !reader.TryReadSemanticTag(out var semanticTag)
            || semanticTag != CborTagConstants.TAG_NONE
        )
        {
            throw new CborException("Expected a CBOR type of NONE");
        }

        return new();
    }

    public override void Write(ref CborWriter writer, None value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_NONE);
        writer.WriteNull();
    }
}
