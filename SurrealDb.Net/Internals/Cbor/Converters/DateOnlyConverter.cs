#if NET6_0_OR_GREATER
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class DateOnlyConverter : CborConverterBase<DateOnly>
{
    public override DateOnly Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size > 2)
        {
            throw new CborException("Expected a CBOR array with at most 2 elements");
        }

        long seconds = size >= 1 ? reader.ReadInt64() : 0;
        int nanos = size >= 2 ? reader.ReadInt32() : 0;

        return DateOnlyParser.Convert(seconds, nanos);
    }

    public override void Write(ref CborWriter writer, DateOnly value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_CUSTOM_DATETIME);

        long seconds = DateOnlyFormatter.Convert(value);

        writer.WriteBeginArray(2);

        writer.WriteInt64(seconds);
        writer.WriteInt32(0);

        writer.WriteEndArray(2);
    }
}
#endif
