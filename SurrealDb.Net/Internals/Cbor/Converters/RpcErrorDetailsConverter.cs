using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Errors;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RpcErrorDetailsConverter : CborConverterBase<RpcErrorDetails>
{
    private readonly ICborConverter<RpcNestedErrorDetails> _rpcNestedErrorDetailsConverter;

    public RpcErrorDetailsConverter(CborOptions options)
    {
        _rpcNestedErrorDetailsConverter =
            options.Registry.ConverterRegistry.Lookup<RpcNestedErrorDetails>();
    }

    public override RpcErrorDetails Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? kind = null;
        RpcNestedErrorDetails? details = null;

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
                details = _rpcNestedErrorDetailsConverter.Read(ref reader);
                continue;
            }

            throw new CborException(
                $"{Encoding.UTF8.GetString(key)} is not a valid property of {nameof(RpcErrorDetails)}."
            );
        }

        return new RpcErrorDetails { Kind = kind!, Details = details };
    }

    public override void Write(ref CborWriter writer, RpcErrorDetails value)
    {
        throw new NotSupportedException($"Cannot write {nameof(RpcErrorDetails)} back in cbor...");
    }
}
