using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbHttpErrorResponseContentConverter
    : CborConverterBase<SurrealDbHttpErrorResponseContent>
{
    public override SurrealDbHttpErrorResponseContent Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        long code = 0;
        string? message = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("code"u8))
            {
                code = reader.ReadInt64();
                continue;
            }

            if (key.SequenceEqual("message"u8))
            {
                message = reader.ReadString();
                continue;
            }

            throw new CborException(
                $"{Encoding.Unicode.GetString(key)} is not a valid property of {nameof(SurrealDbHttpErrorResponseContent)}."
            );
        }

        return new SurrealDbHttpErrorResponseContent { Code = code, Message = message! };
    }

    public override void Write(ref CborWriter writer, SurrealDbHttpErrorResponseContent value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(SurrealDbHttpErrorResponseContent)} back in cbor..."
        );
    }
}
