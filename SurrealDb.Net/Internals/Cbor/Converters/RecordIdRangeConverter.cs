using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdRangeConverter<TStart, TEnd>
    : CborConverterBase<RecordIdRange<TStart, TEnd>>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<Range<TStart, TEnd>> _rangeConverter;

    public RecordIdRangeConverter(CborOptions options)
    {
        _options = options;

        _rangeConverter = options.Registry.ConverterRegistry.Lookup<Range<TStart, TEnd>>();
    }

    public override RecordIdRange<TStart, TEnd> Read(ref CborReader reader)
    {
        throw new NotSupportedException(
            $"Cannot read {nameof(RecordIdRange<TStart, TEnd>)} from cbor..."
        );
    }

    public override void Write(ref CborWriter writer, RecordIdRange<TStart, TEnd> value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);

        writer.WriteBeginArray(2);

        writer.WriteString(value.Table);
        _rangeConverter.Write(ref writer, value.Range);
    }
}
