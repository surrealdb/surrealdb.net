using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Errors;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RpcErrorResponseContentConverter : CborConverterBase<RpcErrorResponseContent>
{
    private readonly ICborConverter<RpcErrorDetails> _rpcErrorDetailsConverter;

    public RpcErrorResponseContentConverter(CborOptions options)
    {
        _rpcErrorDetailsConverter = options.Registry.ConverterRegistry.Lookup<RpcErrorDetails>();
    }

    public override RpcErrorResponseContent Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        long code = 0;
        string? message = null;
        string? kind = null;
        RpcErrorDetails? customErrorDetails = null;

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

            if (key.SequenceEqual("kind"u8))
            {
                kind = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("details"u8))
            {
                customErrorDetails = _rpcErrorDetailsConverter.Read(ref reader);
                continue;
            }

            throw new CborException(
                $"{Encoding.UTF8.GetString(key)} is not a valid property of {nameof(RpcErrorResponseContent)}."
            );
        }

        return new RpcErrorResponseContent
        {
            Code = code,
            Message = message!,
            Kind = kind,
            Details = customErrorDetails,
        };
    }

    public override void Write(ref CborWriter writer, RpcErrorResponseContent value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(RpcErrorResponseContent)} back in cbor..."
        );
    }
}
