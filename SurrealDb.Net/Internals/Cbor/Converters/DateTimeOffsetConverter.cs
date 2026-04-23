using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class DateTimeOffsetConverter : CborConverterBase<DateTimeOffset>
{
    public override DateTimeOffset Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size > 2)
        {
            throw new CborException("Expected a CBOR array with at most 2 elements");
        }

        long seconds = size >= 1 ? reader.ReadInt64() : 0;
        int nanos = size >= 2 ? reader.ReadInt32() : 0;

        // SurrealDB datetimes are timezone-independent instants. We normalize to UTC.
        return new DateTimeOffset(DateTimeParser.Convert(seconds, nanos));
    }

    public override void Write(ref CborWriter writer, DateTimeOffset value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_CUSTOM_DATETIME);

        var (seconds, nanos) = DateTimeFormatter.Convert(value.UtcDateTime);

        writer.WriteBeginArray(2);

        writer.WriteInt64(seconds);
        writer.WriteInt32(nanos);

        writer.WriteEndArray(2);
    }
}
