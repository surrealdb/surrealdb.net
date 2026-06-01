using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Errors;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class SurrealDbHttpResponseConverter : CborConverterBase<ISurrealDbHttpResponse>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<RpcErrorResponseContent> _rpcErrorResponseContentConverter;
    private readonly ICborConverter<Guid> _guidConverter;

    public SurrealDbHttpResponseConverter(CborOptions options)
    {
        _options = options;

        _rpcErrorResponseContentConverter =
            options.Registry.ConverterRegistry.Lookup<RpcErrorResponseContent>();
        _guidConverter = options.Registry.ConverterRegistry.Lookup<Guid>();
    }

    public override ISurrealDbHttpResponse Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        RpcErrorResponseContent? errorContent = null;
        ReadOnlyMemory<byte>? result = null;
        Guid? session = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("error"u8))
            {
                errorContent = _rpcErrorResponseContentConverter.Read(ref reader);
                continue;
            }

            if (key.SequenceEqual("result"u8))
            {
                result = reader.ReadDataItemAsMemory();
                continue;
            }

            if (key.SequenceEqual("session"u8))
            {
                session = _guidConverter.Read(ref reader);
                continue;
            }

            throw new CborException(
                $"{Encoding.UTF8.GetString(key)} is not a valid property of {nameof(ISurrealDbHttpResponse)}."
            );
        }

        if (result.HasValue)
        {
            return new SurrealDbHttpOkResponse(result.Value, _options);
        }

        if (errorContent is not null)
        {
            return new SurrealDbHttpErrorResponse { Error = errorContent };
        }

        return new SurrealDbHttpUnknownResponse();
    }

    public override void Write(ref CborWriter writer, ISurrealDbHttpResponse value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(ISurrealDbHttpResponse)} back in cbor..."
        );
    }
}
