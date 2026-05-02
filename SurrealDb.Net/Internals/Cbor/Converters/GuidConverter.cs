using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class GuidConverter : CborConverterBase<Guid>
{
    private const int CBOR_ARRAY_SIZE = 16;

    public override Guid Read(ref CborReader reader)
    {
#if NET8_0_OR_GREATER
        // Do NOT call TryReadSemanticTag before GetCurrentDataItemType.
        // TryReadSemanticTag sets _state = CborReaderState.Data after consuming the tag.
        // GetCurrentDataItemType then calls SkipSemanticTag → GetHeader, which — because
        // state is Data — reads one extra byte from the stream, misaligning the cursor.
        // This produces the "[1] Expected major type ByteString (2)" error at offset 1.
        //
        // Instead, let GetCurrentDataItemType handle tag-skipping on its own via its
        // internal SkipSemanticTag call, which leaves the cursor on the value header
        // in CborReaderState.Header (peeked but not consumed).
        var dataItemType = reader.GetCurrentDataItemType();

        // SurrealDB v2 and earlier: tag(37, bstr(16)) — 16-byte big-endian UUID.
        // SurrealDB v3+:            tag(37, tstr)     — UUID as a text string.
        if (dataItemType == CborDataItemType.String)
        {
            return Guid.Parse(reader.ReadString()!);
        }

        var value = reader.ReadByteString();

        if (value.Length != CBOR_ARRAY_SIZE)
        {
            throw new CborException("Expected a CBOR byte array with 16 elements");
        }

        return new Guid(value, true);
#else
        throw new NotSupportedException(
            "Guid serialization is only supported in .NET 8.0 and above."
        );
#endif
    }

    public override void Write(ref CborWriter writer, Guid value)
    {
#if NET8_0_OR_GREATER
        writer.WriteSemanticTag(CborTagConstants.TAG_UUID);

        Span<byte> bytes = new byte[CBOR_ARRAY_SIZE];
        if (value.TryWriteBytes(bytes, true, out _))
        {
            writer.WriteByteString(bytes);
        }
        else
        {
            throw new CborException("Cannot serialize the GUID to CBOR");
        }
#else
        throw new NotSupportedException(
            "Guid serialization is only supported in .NET 8.0 and above."
        );
#endif
    }
}
