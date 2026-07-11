using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class OrderedEnumerableConverter<T> : CborConverterBase<IOrderedEnumerable<T>>
{
    private readonly CborOptions _options;

    public OrderedEnumerableConverter(CborOptions options)
    {
        _options = options;
    }

    public override IOrderedEnumerable<T> Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();
        var items = new List<T>(size);

        for (int index = 0; index < size; index++)
        {
            items.Add(CborSerializer.Deserialize<T>(reader.ReadDataItem(), _options));
        }

        return new MaterializedOrderedEnumerable<T>(items);
    }

    public override void Write(ref CborWriter writer, IOrderedEnumerable<T> value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var items = value.ToArray();
        writer.WriteBeginArray(items.Length);

        foreach (var item in items)
        {
            CborSerializer.Serialize(item, writer.BufferWriter, _options);
        }
    }
}
