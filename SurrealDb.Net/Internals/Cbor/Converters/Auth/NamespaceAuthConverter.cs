using Dahomey.Cbor.Serialization;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Cbor.Converters.Auth;

internal sealed class NamespaceAuthConverter : AuthConverterBase<NamespaceAuth>
{
    protected override bool TryReadProperty(
        ref CborReader reader,
        ReadOnlySpan<byte> key,
        NamespaceAuth value
    )
    {
        if (key.SequenceEqual("ns"u8))
        {
            value.Namespace = reader.ReadString()!;
            return true;
        }

        return SystemAuthProperties.TryRead(ref reader, key, value);
    }

    protected override void WriteProperties(ref CborWriter writer, NamespaceAuth value)
    {
        writer.WriteBeginMap(1 + SystemAuthProperties.Count);
        WriteProperty(ref writer, NamespacePropertyName, value.Namespace);
        SystemAuthProperties.Write(ref writer, value);
    }
}
