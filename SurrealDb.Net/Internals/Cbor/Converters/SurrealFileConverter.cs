using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class SurrealFileConverter : CborConverterBase<SurrealFile>
{
    public override SurrealFile Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException("Expected a CBOR array with two String bucket and key values");
        }

        string bucket =
            reader.ReadString()
            ?? throw new CborException("Expected the bucket name to be a string value");
        string path =
            reader.ReadString()
            ?? throw new CborException("Expected the file key to be a string value");

        return new SurrealFile(bucket, path);
    }

    public override void Write(ref CborWriter writer, SurrealFile value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_FILE);

        writer.WriteBeginArray(2);

        writer.WriteString(value.Bucket);
        writer.WriteString(value.Path);

        writer.WriteEndArray(2);
    }
}
