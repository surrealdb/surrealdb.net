using Dahomey.Cbor.Serialization;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Cbor.Converters.Auth;

internal sealed class DatabaseAuthConverter : AuthConverterBase<DatabaseAuth>
{
    protected override bool TryReadProperty(
        ref CborReader reader,
        ReadOnlySpan<byte> key,
        DatabaseAuth value
    )
    {
        if (key.SequenceEqual("ns"u8))
        {
            value.Namespace = reader.ReadString()!;
            return true;
        }

        if (key.SequenceEqual("db"u8))
        {
            value.Database = reader.ReadString()!;
            return true;
        }

        return SystemAuthProperties.TryRead(ref reader, key, value);
    }

    protected override void WriteProperties(ref CborWriter writer, DatabaseAuth value)
    {
        writer.WriteBeginMap(2 + SystemAuthProperties.Count);
        WriteProperty(ref writer, NamespacePropertyName, value.Namespace);
        WriteProperty(ref writer, DatabasePropertyName, value.Database);
        SystemAuthProperties.Write(ref writer, value);
    }
}
