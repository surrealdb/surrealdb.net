using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class RecordIdOfStringConverter : CborConverterBase<RecordIdOfString>
{
    public override RecordIdOfString Read(ref CborReader reader)
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
        string id =
            reader.ReadString()
            ?? throw new CborException("Expected a string as the second element of the array");

        return new RecordIdOfString(table, id);
    }

    public override void Write(ref CborWriter writer, RecordIdOfString value)
    {
        WriteRecordIdOfString(ref writer, value);
    }

    internal static void WriteRecordIdOfString(ref CborWriter writer, RecordIdOfString value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteString(value.Table);
        writer.WriteString(value.Id);
    }
}
