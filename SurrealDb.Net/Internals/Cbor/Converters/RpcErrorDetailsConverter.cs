using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Errors;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RpcErrorDetailsConverter : CborConverterBase<RpcErrorDetails>
{
    public override RpcErrorDetails Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? kind = null;
        IReadOnlyDictionary<string, object?>? details = null;

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
                details = CborMapToDictionaryConverter.ReadNullableMap(ref reader);
                continue;
            }

            reader.SkipDataItem();
        }

        return new RpcErrorDetails { Kind = kind ?? string.Empty, Details = details };
    }

    public override void Write(ref CborWriter writer, RpcErrorDetails value)
    {
        throw new NotSupportedException($"Cannot write {nameof(RpcErrorDetails)} back in cbor...");
    }
}
