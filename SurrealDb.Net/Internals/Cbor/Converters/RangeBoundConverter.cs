using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RangeBoundConverter<T> : CborConverterBase<RangeBound<T>>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<T> _valueConverter;

    public RangeBoundConverter(CborOptions options)
    {
        _options = options;

        _valueConverter = options.Registry.ConverterRegistry.Lookup<T>();
    }

    public override RangeBound<T> Read(ref CborReader reader)
    {
        if (!reader.TryReadSemanticTag(out var semanticTag) || !IsValidSemanticTag(semanticTag))
        {
            throw new CborException("Expected a CBOR type of RangeBound");
        }

        var type =
            semanticTag == CborTagConstants.TAG_EXCLUSIVE_BOUND
                ? RangeBoundType.Exclusive
                : RangeBoundType.Inclusive;

        var value = _valueConverter.Read(ref reader);

        return new RangeBound<T>(value, type);
    }

    private static bool IsValidSemanticTag(ulong semanticTag)
    {
        return semanticTag == CborTagConstants.TAG_INCLUSIVE_BOUND
            || semanticTag == CborTagConstants.TAG_EXCLUSIVE_BOUND;
    }

    public override void Write(ref CborWriter writer, RangeBound<T> value)
    {
        switch (value.Type)
        {
            case RangeBoundType.Inclusive:
                writer.WriteSemanticTag(CborTagConstants.TAG_INCLUSIVE_BOUND);
                break;
            case RangeBoundType.Exclusive:
                writer.WriteSemanticTag(CborTagConstants.TAG_EXCLUSIVE_BOUND);
                break;
            default:
                throw new NotImplementedException("The range bound type is not currently handled.");
        }

        _valueConverter.Write(ref writer, value.Value);
    }
}
