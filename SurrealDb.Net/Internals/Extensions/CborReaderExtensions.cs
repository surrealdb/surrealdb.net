using Dahomey.Cbor.Serialization;

namespace SurrealDb.Net.Internals.Extensions;

internal static class CborReaderExtensions
{
    public static ReadOnlyMemory<byte> ReadDataItemAsMemory(this ref CborReader reader)
    {
        var span = reader.ReadDataItem();
        return span.ToMemory();
    }
}
