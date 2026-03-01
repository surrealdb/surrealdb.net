using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Errors;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RpcNestedErrorDetailsConverter : CborConverterBase<RpcNestedErrorDetails>
{
    public override RpcNestedErrorDetails Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? kind = null;
        ReadOnlyMemory<byte>? details = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("kind"u8))
            {
                kind = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("details"u8))
            {
                details = reader.ReadDataItemAsMemory();
                continue;
            }

            throw new CborException(
                $"{Encoding.UTF8.GetString(key)} is not a valid property of {nameof(RpcNestedErrorDetails)}."
            );
        }

        return new RpcNestedErrorDetails { Kind = kind!, Details = details };
    }

    public override void Write(ref CborWriter writer, RpcNestedErrorDetails value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(RpcNestedErrorDetails)} back in cbor..."
        );
    }
}
