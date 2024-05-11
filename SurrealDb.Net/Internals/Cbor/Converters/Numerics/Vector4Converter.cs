using System.Numerics;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class Vector4Converter : CborConverterBase<Vector4>
{
    private const byte VECTOR_LENGTH = 4;

    public override Vector4 Read(ref CborReader reader)
    {
        var itemType = reader.GetCurrentDataItemType();

        if (itemType == CborDataItemType.Null)
        {
            reader.ReadNull();
            return default;
        }

        if (itemType == CborDataItemType.Array)
        {
            reader.ReadBeginArray();

            int size = reader.ReadSize();

            if (size < VECTOR_LENGTH)
            {
                throw new CborException("Expected a CBOR array with at least 4 elements");
            }

            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = reader.ReadSingle();

            for (int i = VECTOR_LENGTH; i < size; i++)
            {
                reader.SkipDataItem();
            }

            return new Vector4(x, y, z, w);
        }

        throw new CborException("Expected a CBOR array with at least 4 elements");
    }

    public override void Write(ref CborWriter writer, Vector4 value)
    {
        writer.WriteBeginArray(VECTOR_LENGTH);

        writer.WriteSingle(value.X);
        writer.WriteSingle(value.Y);
        writer.WriteSingle(value.Z);
        writer.WriteSingle(value.W);
    }
}
