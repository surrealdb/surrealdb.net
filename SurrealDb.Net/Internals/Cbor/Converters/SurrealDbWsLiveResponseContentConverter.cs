using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbWsLiveResponseContentConverter
    : CborConverterBase<SurrealDbWsLiveResponseContent>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<Guid> _guidConverter;

    public SurrealDbWsLiveResponseContentConverter(CborOptions options)
    {
        _options = options;

        _guidConverter = options.Registry.ConverterRegistry.Lookup<Guid>();
    }

    public override SurrealDbWsLiveResponseContent Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        Guid? id = default;
        string? action = null;
        ReadOnlyMemory<byte> result = default;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            var key = reader.ReadString();

            switch (key)
            {
                case SurrealDbWsResponseConstants.IdPropertyName:
                    id = _guidConverter.Read(ref reader);
                    break;
                case SurrealDbWsResponseConstants.ActionPropertyName:
                    action = reader.ReadString();
                    break;
                case SurrealDbWsResponseConstants.ResultPropertyName:
                    result = reader.ReadDataItemAsMemory();
                    break;
                default:
                    throw new CborException(
                        $"{key} is not a valid property of {nameof(SurrealDbWsLiveResponseContent)}."
                    );
            }
        }

        if (!result.IsEmpty && id.HasValue && action is not null)
        {
            return new SurrealDbWsLiveResponseContent(id.Value, action!, result, _options);
        }

        throw new CborException("Expected a valid response content");
    }

    public override void Write(ref CborWriter writer, SurrealDbWsLiveResponseContent value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(SurrealDbWsLiveResponseContent)} back in cbor..."
        );
    }
}
