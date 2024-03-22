using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Constants;
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
        ReadOnlyMemory<byte> result = default;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            var key = reader.ReadString();

            switch (key)
            {
                case SurrealDbWsResponseConstants.IdPropertyName:
                    rootId = reader.ReadString();
                    break;
                case SurrealDbWsResponseConstants.ErrorPropertyName:
                    errorContent = _surrealDbWsErrorResponseContentConverter.Read(ref reader);
                    break;
                case SurrealDbWsResponseConstants.ResultPropertyName:
                    result = reader.ReadDataItemAsMemory();
                    break;
                default:
                    throw new CborException(
                        $"{key} is not a valid property of {nameof(ISurrealDbWsResponse)}."
                    );
            }
        }

        if (rootId is not null)
        {
            if (!result.IsEmpty)
            {
                return new SurrealDbWsOkResponse(rootId, result, _options);
            }

            if (errorContent is not null)
            {
                return new SurrealDbWsErrorResponse { Id = rootId, Error = errorContent };
            }

            return new SurrealDbWsUnknownResponse();
        }

        if (!result.IsEmpty)
        {
            var content = CborSerializer.Deserialize<SurrealDbWsLiveResponseContent>(
                result.Span,
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
