#if NET6_0_OR_GREATER
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class DateOnlyConverter : CborConverterBase<DateOnly>
{
    private const int CBOR_ARRAY_SIZE = 2;

    public override DateOnly Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != CBOR_ARRAY_SIZE)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        long seconds = reader.ReadInt64();
        int nanos = reader.ReadInt32();

        return DateOnlyParser.Convert(seconds, nanos);
    }

    public override void Write(ref CborWriter writer, DateOnly value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_CUSTOM_DATETIME);

        long seconds = DateOnlyFormatter.Convert(value);

        writer.WriteBeginArray(CBOR_ARRAY_SIZE);

        writer.WriteInt64(seconds);
        writer.WriteInt32(0);

        writer.WriteEndArray(CBOR_ARRAY_SIZE);
    }
}
#endif
