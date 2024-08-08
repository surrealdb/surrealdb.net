using System.Numerics;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class Vector3Converter : CborConverterBase<Vector3>
{
    private const byte VECTOR_LENGTH = 3;

    public override Vector3 Read(ref CborReader reader)
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
                throw new CborException("Expected a CBOR array with at least 3 elements");
            }

            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();

            for (int i = VECTOR_LENGTH; i < size; i++)
            {
                reader.SkipDataItem();
            }

            return new Vector3(x, y, z);
        }

        throw new CborException("Expected a CBOR array with at least 3 elements");
    }

    public override void Write(ref CborWriter writer, Vector3 value)
    {
        writer.WriteBeginArray(VECTOR_LENGTH);

        writer.WriteSingle(value.X);
        writer.WriteSingle(value.Y);
        writer.WriteSingle(value.Z);
    }
}
