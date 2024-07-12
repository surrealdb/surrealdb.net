using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbWsErrorResponseContentConverter
    : CborConverterBase<SurrealDbWsErrorResponseContent>
{
    public override SurrealDbWsErrorResponseContent Read(ref CborReader reader)
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
                $"{Encoding.Unicode.GetString(key)} is not a valid property of {nameof(SurrealDbWsErrorResponseContent)}."
            );
        }

        return new SurrealDbWsErrorResponseContent { Code = code, Message = message! };
    }

    public override void Write(ref CborWriter writer, SurrealDbWsErrorResponseContent value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(SurrealDbWsErrorResponseContent)} back in cbor..."
        );
    }
}
