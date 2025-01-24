using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdOfTConverter<T> : CborConverterBase<RecordIdOf<T>>
{
    private readonly CborOptions _options;

    public RecordIdOfTConverter(CborOptions options)
    {
        _options = options;
    }

    public override RecordIdOf<T> Read(ref CborReader reader)
    {
        return reader.GetCurrentDataItemType() switch
        {
            CborDataItemType.Null => default!,
            CborDataItemType.Array => ReadRecordIdFromArray(ref reader),
            CborDataItemType.String => throw new CborException(
                $"The type '{nameof(StringRecordId)}' was not expected here"
            ),
            _ => throw new CborException(
                "Expected a CBOR text data type, or a CBOR array with 2 elements"
            ),
        };
    }

    private RecordIdOf<T> ReadRecordIdFromArray(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException(
                "Expected a CBOR text data type, or a CBOR array with 2 elements"
            );
        }

        string table =
            reader.ReadString()
            ?? throw new CborException("Expected a string as the first element of the array");
        var id = CborSerializer.Deserialize<T>(reader.ReadDataItem(), _options);

        return new RecordIdOf<T>(table, id);
    }

    public override void Write(ref CborWriter writer, RecordIdOf<T> value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteString(value.Table);
        CborSerializer.Serialize(value.Id, writer.BufferWriter, _options);
    }
}
