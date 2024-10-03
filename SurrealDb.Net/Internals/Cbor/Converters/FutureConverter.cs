using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class FutureConverter : CborConverterBase<Future>
{
    public override Future Read(ref CborReader reader)
    {
        if (
            !reader.TryReadSemanticTag(out var semanticTag)
            || semanticTag != CborTagConstants.TAG_FUTURE
        )
        {
            throw new CborException("Expected a CBOR type of future");
        }

        string? inner =
            reader.ReadString() ?? throw new CborException("Expected a CBOR type of future");

        return new(inner);
    }

    public override void Write(ref CborWriter writer, Future value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_FUTURE);
        writer.WriteString(value.Inner);
    }
}
