using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class HashSetConverter<T> : CborConverterBase<HashSet<T>>
{
    private readonly CborOptions _options;

    public HashSetConverter(CborOptions options)
    {
        _options = options;
    }

    public override HashSet<T> Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var value = new HashSet<T>(size);

        for (int index = 0; index < size; index++)
        {
            value.Add(CborSerializer.Deserialize<T>(reader.ReadDataItem(), _options));
        }

        return value;
    }

    public override void Write(ref CborWriter writer, HashSet<T> value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteSemanticTag(CborTagConstants.TAG_SET);

        writer.WriteBeginArray(value.Count);

        foreach (var item in value)
        {
            CborSerializer.Serialize(item, writer.BufferWriter, _options);
        }
    }
}
