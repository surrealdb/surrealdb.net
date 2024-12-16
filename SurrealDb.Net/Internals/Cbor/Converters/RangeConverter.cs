using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RangeConverter<TStart, TEnd> : CborConverterBase<Range<TStart, TEnd>>
{
    private const int CBOR_ARRAY_SIZE = 2;

    private readonly CborOptions _options;
    private readonly ICborConverter<RangeBound<TStart>> _rangeStartBoundConverter;
    private readonly ICborConverter<RangeBound<TEnd>> _rangeEndBoundConverter;

    public RangeConverter(CborOptions options)
    {
        _options = options;

        _rangeStartBoundConverter = options.Registry.ConverterRegistry.Lookup<RangeBound<TStart>>();
        _rangeEndBoundConverter = options.Registry.ConverterRegistry.Lookup<RangeBound<TEnd>>();
    }

    public override Range<TStart, TEnd> Read(ref CborReader reader)
    {
        if (
            !reader.TryReadSemanticTag(out var semanticTag)
            || semanticTag != CborTagConstants.TAG_RANGE
        )
        {
            throw new CborException("Expected a CBOR type of Range");
        }

        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != CBOR_ARRAY_SIZE)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        var start = _rangeStartBoundConverter.Read(ref reader);
        var end = _rangeEndBoundConverter.Read(ref reader);

        return new(start, end);
    }

    public override void Write(ref CborWriter writer, Range<TStart, TEnd> value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RANGE);

        writer.WriteBeginArray(CBOR_ARRAY_SIZE);

        if (value.Start.HasValue)
        {
            _rangeStartBoundConverter.Write(ref writer, value.Start.Value);
        }
        else
        {
            writer.WriteNull();
        }

        if (value.End.HasValue)
        {
            _rangeEndBoundConverter.Write(ref writer, value.End.Value);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
