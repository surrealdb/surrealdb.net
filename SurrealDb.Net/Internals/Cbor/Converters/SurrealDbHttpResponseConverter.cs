using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbHttpResponseConverter : CborConverterBase<ISurrealDbHttpResponse>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<SurrealDbHttpErrorResponseContent> _surrealDbHttpErrorResponseContentConverter;

    public SurrealDbHttpResponseConverter(CborOptions options)
    {
        _options = options;

        _surrealDbHttpErrorResponseContentConverter =
            options.Registry.ConverterRegistry.Lookup<SurrealDbHttpErrorResponseContent>();
    }

    public override ISurrealDbHttpResponse Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        SurrealDbHttpErrorResponseContent? errorContent = null;
        ReadOnlyMemory<byte>? result = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("error"u8))
            {
                errorContent = _surrealDbHttpErrorResponseContentConverter.Read(ref reader);
                continue;
            }

            if (key.SequenceEqual("result"u8))
            {
                result = reader.ReadDataItemAsMemory();
                continue;
            }

            throw new CborException(
                $"{Encoding.Unicode.GetString(key)} is not a valid property of {nameof(ISurrealDbHttpResponse)}."
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
