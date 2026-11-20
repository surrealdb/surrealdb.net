using Dahomey.Cbor.Serialization;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Cbor.Converters.Auth;

internal sealed class RootAuthConverter : AuthConverterBase<RootAuth>
{
    protected override bool TryReadProperty(
        ref CborReader reader,
        ReadOnlySpan<byte> key,
        RootAuth value
    )
    {
        return SystemAuthProperties.TryRead(ref reader, key, value);
    }

    protected override void WriteProperties(ref CborWriter writer, RootAuth value)
    {
        writer.WriteBeginMap(SystemAuthProperties.Count);
        SystemAuthProperties.Write(ref writer, value);
    }
}
