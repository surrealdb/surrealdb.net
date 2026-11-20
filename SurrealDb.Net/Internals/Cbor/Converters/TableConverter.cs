using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class TableConverter : CborConverterBase<Table>
{
    public override Table Read(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() != CborDataItemType.String)
        {
            throw new CborException("Expected a CBOR text data type");
        }

        return new Table(reader.ReadString()!);
    }

    public override void Write(ref CborWriter writer, Table value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_TABLE);
        writer.WriteString(value.Name);
    }
}
