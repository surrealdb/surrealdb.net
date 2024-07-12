using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class StringRecordIdConverter : CborConverterBase<StringRecordId>
{
    public override StringRecordId Read(ref CborReader reader)
    {
        throw new NotSupportedException($"Cannot read {nameof(StringRecordId)} from cbor...");
    }

    public override void Write(ref CborWriter writer, StringRecordId value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);
        writer.WriteString(value.Value);
    }
}
