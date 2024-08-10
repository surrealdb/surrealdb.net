using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class DateTimeOffsetConverter : CborConverterBase<DateTimeOffset>
{
    private const int CBOR_ARRAY_SIZE = 2;

    public override DateTimeOffset Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != CBOR_ARRAY_SIZE)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        long seconds = reader.ReadInt64();
        int nanos = reader.ReadInt32();

        return DateTimeParser.ConvertOffset(seconds, nanos);
    }

    public override void Write(ref CborWriter writer, DateTimeOffset value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_CUSTOM_DATETIME);

        var (seconds, nanos) = DateTimeFormatter.Convert(value);

        writer.WriteBeginArray(CBOR_ARRAY_SIZE);

        writer.WriteInt64(seconds);
        writer.WriteInt32(nanos);

        writer.WriteEndArray(CBOR_ARRAY_SIZE);
    }
}
