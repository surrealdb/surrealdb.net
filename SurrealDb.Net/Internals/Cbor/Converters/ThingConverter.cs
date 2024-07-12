using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class ThingConverter : CborConverterBase<Thing>
{
    public override Thing Read(ref CborReader reader)
    {
        return reader.GetCurrentDataItemType() switch
        {
            CborDataItemType.Null => default!,
            CborDataItemType.String => new Thing(reader.ReadString()!),
            CborDataItemType.Array => ReadThingFromArray(ref reader),
            _
                => throw new CborException(
                    "Expected a CBOR text data type, or a CBOR array with 2 elements"
                )
        };
    }

    private static Thing ReadThingFromArray(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException(
                "Expected a CBOR text data type, or a CBOR array with 2 elements"
            );
        }

        var table = reader.ReadString();

        if (table is null)
        {
            throw new CborException("Expected a string as the first element of the array");
        }

        var idItemType = reader.GetCurrentDataItemType();

        return idItemType switch
        {
            CborDataItemType.String => Thing.From(table, reader.ReadString()!),
            CborDataItemType.Signed
            or CborDataItemType.Unsigned
                => Thing.From(table, reader.ReadInt32()),
            CborDataItemType.Array => Thing.From(table, reader.ReadDataItemAsMemory()),
            CborDataItemType.Map => Thing.From(table, reader.ReadDataItemAsMemory()),
            _
                => throw new CborException(
                    "Expected the id of a Record Id to be a String, Integer, Array or Object value"
                )
        };
    }

    public override void Write(ref CborWriter writer, Thing value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_RECORDID);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteString(value.ToString());
    }
}
