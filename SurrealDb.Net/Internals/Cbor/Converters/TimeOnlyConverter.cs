#if NET6_0_OR_GREATER
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class TimeOnlyConverter : CborConverterBase<TimeOnly>
{
    public override TimeOnly Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size > 2)
        {
            throw new CborException("Expected a CBOR array with at most 2 elements");
        }

        long? seconds = size >= 1 ? reader.ReadInt64() : null;
        int? nanos = size >= 2 ? reader.ReadInt32() : null;

        return TimeOnlyParser.Convert(seconds, nanos);
    }

    public override void Write(ref CborWriter writer, TimeOnly value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_CUSTOM_DURATION);

        var (seconds, nanos) = TimeOnlyFormatter.Convert(value);

        bool writeNanos = nanos != 0;
        bool writeSeconds = writeNanos || seconds != 0;

        int size = (writeSeconds ? 1 : 0) + (writeNanos ? 1 : 0);

        writer.WriteBeginArray(size);

        if (writeSeconds)
        {
            writer.WriteInt64(seconds);
        }
        if (writeNanos)
        {
            writer.WriteInt32(nanos);
        }

        writer.WriteEndArray(size);
    }
}
#endif
