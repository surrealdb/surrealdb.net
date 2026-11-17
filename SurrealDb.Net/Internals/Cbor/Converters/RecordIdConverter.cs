using System.Buffers;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdConverter : CborConverterBase<RecordId>
{
    private readonly CborOptions _options;

    public RecordIdConverter(CborOptions options)
    {
        _options = options;
    }

    public override RecordId Read(ref CborReader reader)
    {
        var dataType = reader.GetCurrentDataItemType();

        if (dataType == CborDataItemType.Null)
        {
            reader.ReadNull();
            return default!;
        }

        return dataType switch
        {
            CborDataItemType.Array => ReadRecordIdFromArray(ref reader),
            CborDataItemType.String => throw new CborException(
                $"The type '{nameof(StringRecordId)}' was not expected here"
            ),
            _ => throw new CborException(
                "Expected a CBOR text data type, or a CBOR array with 2 elements"
            ),
        };
    }

    private RecordId ReadRecordIdFromArray(ref CborReader reader)
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

        var idDataItem = reader.ReadDataItem();

        // TODO : MoveToPreviousDataItem method?
        var idReader = new CborReader(idDataItem);
        var idItemType = idReader.GetCurrentDataItemType();

        return idItemType switch
        {
            CborDataItemType.String => new RecordIdOfString(table, idReader.ReadString()!),
            CborDataItemType.Signed or CborDataItemType.Unsigned => FromIntegerId(
                table,
                idReader.ReadInt64()
            ),
            _ => new RecordId(table, idDataItem.ToMemory(), _options),
        };
    }

    /// <summary>
    /// SurrealDB integers are 64-bit. Ids that fit in 32 bits stay <see cref="RecordIdOf{TId}"/> of <see cref="int"/>.
    /// </summary>
    private static RecordId FromIntegerId(string table, long id)
    {
        return id is >= int.MinValue and <= int.MaxValue
            ? new RecordIdOf<int>(table, (int)id)
            : new RecordIdOf<long>(table, id);
    }

    public override void Write(ref CborWriter writer, RecordId value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (value is RecordIdOfString recordIdOfString)
        {
            RecordIdOfStringConverter.WriteRecordIdOfString(ref writer, recordIdOfString);
            return;
        }

        var type = value.GetType();
        if (type.Namespace == ConverterTypeConstants.ModelsNamespace && type.Name == "RecordIdOf`1")
        {
            var idType = type.GenericTypeArguments[0];
            WriteGenericRecordId(
                ref writer,
                value.Table,
                value.DeserializeId(idType),
                idType,
                _options
            );
            return;
        }

        WriteSerializedRecordId(ref writer, value);
    }

    private static void WriteSerializedRecordId(ref CborWriter writer, RecordId value)
    {
        if (!value._serializedCborId.HasValue)
        {
            throw new CborException($"Cannot serialize a {nameof(RecordId)}.");
        }

        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteString(value.Table);
        writer.BufferWriter.Write(value._serializedCborId.Value.Span);
    }

    private static void WriteGenericRecordId(
        ref CborWriter writer,
        string table,
        object? value,
        Type idType,
        CborOptions options
    )
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteString(table);
        CborSerializer.Serialize(value, idType, writer.BufferWriter, options);
    }
}
