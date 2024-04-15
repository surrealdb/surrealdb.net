using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbWsResponseConverter : CborConverterBase<ISurrealDbWsResponse>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<SurrealDbWsErrorResponseContent> _surrealDbWsErrorResponseContentConverter;

    public SurrealDbWsResponseConverter(CborOptions options)
    {
        _options = options;

        _surrealDbWsErrorResponseContentConverter =
            options.Registry.ConverterRegistry.Lookup<SurrealDbWsErrorResponseContent>();
    }

    public override ISurrealDbWsResponse Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? rootId = null;
        SurrealDbWsErrorResponseContent? errorContent = null;
        ReadOnlyMemory<byte>? result = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("id"u8))
            {
                rootId = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("error"u8))
            {
                errorContent = _surrealDbWsErrorResponseContentConverter.Read(ref reader);
                continue;
            }

            if (key.SequenceEqual("result"u8))
            {
                result = reader.ReadDataItemAsMemory();
                continue;
            }

            throw new CborException(
                $"{Encoding.Unicode.GetString(key)} is not a valid property of {nameof(ISurrealDbWsResponse)}."
            );
        }

        if (rootId is not null)
        {
            if (result.HasValue)
            {
                return new SurrealDbWsOkResponse(rootId, result.Value, _options);
            }

            if (errorContent is not null)
            {
                return new SurrealDbWsErrorResponse { Id = rootId, Error = errorContent };
            }

            return new SurrealDbWsUnknownResponse();
        }

        if (result.HasValue)
        {
            var content = CborSerializer.Deserialize<SurrealDbWsLiveResponseContent>(
                result.Value.Span,
                _options
            );
            return new SurrealDbWsLiveResponse(content);
        }

        return new SurrealDbWsUnknownResponse();
    }

    public override void Write(ref CborWriter writer, ISurrealDbWsResponse value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(ISurrealDbWsResponse)} back in cbor..."
        );
    }
}
