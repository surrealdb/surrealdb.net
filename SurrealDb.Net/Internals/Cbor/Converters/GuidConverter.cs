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
