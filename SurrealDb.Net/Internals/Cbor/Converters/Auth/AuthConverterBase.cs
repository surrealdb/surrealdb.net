using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters.Auth;

/// <summary>
/// Base converter for the authentication models sent with <c>signin</c> and <c>signup</c>.
/// The models are written and read as CBOR maps without the reflection-based object converter,
/// so that authentication works under Native AOT.
/// </summary>
internal abstract class AuthConverterBase<T> : CborConverterBase<T>
    where T : class, new()
{
    protected const string NamespacePropertyName = "ns";
    protected const string DatabasePropertyName = "db";

    public override T Read(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null!;
        }

        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();
        var value = new T();

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            var key = reader.ReadRawString();

            if (!TryReadProperty(ref reader, key, value))
            {
                // Skip the value of an unknown property
                reader.ReadDataItem();
            }
        }

        return value;
    }

    public override void Write(ref CborWriter writer, T value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        WriteProperties(ref writer, value);
    }

    /// <summary>
    /// Reads the value of the property named <paramref name="key"/>, if it is known.
    /// </summary>
    /// <returns>True if the property is known and its value was read.</returns>
    protected abstract bool TryReadProperty(ref CborReader reader, ReadOnlySpan<byte> key, T value);

    /// <summary>
    /// Writes the map that represents <paramref name="value"/>.
    /// </summary>
    protected abstract void WriteProperties(ref CborWriter writer, T value);

    protected static void WriteProperty(ref CborWriter writer, string name, string? value)
    {
        writer.WriteString(name);
        writer.WriteString(value);
    }
}
