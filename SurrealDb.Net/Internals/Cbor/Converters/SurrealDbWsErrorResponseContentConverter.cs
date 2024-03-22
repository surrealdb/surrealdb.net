using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Constants;
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
            var key = reader.ReadString();

            switch (key)
            {
                case SurrealDbWsResponseConstants.CodePropertyName:
                    code = reader.ReadInt64();
                    break;
                case SurrealDbWsResponseConstants.MessagePropertyName:
                    message = reader.ReadString();
                    break;
                default:
                    throw new CborException(
                        $"{key} is not a valid property of {nameof(SurrealDbWsErrorResponseContent)}."
                    );
            }
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
