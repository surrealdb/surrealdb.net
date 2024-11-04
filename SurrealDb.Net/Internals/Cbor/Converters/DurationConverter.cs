using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class DurationConverter : CborConverterBase<Duration>
{
    public override Duration Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size > 2)
        {
            throw new CborException("Expected a CBOR array with at most 2 elements");
        }

        long seconds = size >= 1 ? reader.ReadInt64() : 0;
        int nanos = size >= 2 ? reader.ReadInt32() : 0;

        return new Duration(seconds, nanos);
    }

    public override void Write(ref CborWriter writer, Duration value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_CUSTOM_DURATION);

        var (seconds, nanos) = DurationFormatter.Convert(value);

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
