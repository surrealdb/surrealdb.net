using Dahomey.Cbor.Serialization;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Cbor.Converters.Auth;

/// <summary>
/// Reads and writes the properties declared by <see cref="SystemAuth"/>.
/// </summary>
internal static class SystemAuthProperties
{
    public const int Count = 2;

    public static bool TryRead(ref CborReader reader, ReadOnlySpan<byte> key, SystemAuth value)
    {
        if (key.SequenceEqual("user"u8))
        {
            value.Username = reader.ReadString()!;
            return true;
        }

        if (key.SequenceEqual("pass"u8))
        {
            value.Password = reader.ReadString()!;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes the properties of <see cref="SystemAuth"/> as map entries.
    /// Call it after the properties of the derived type, which the reflection-based
    /// object converter also writes first.
    /// </summary>
    public static void Write(ref CborWriter writer, SystemAuth value)
    {
        writer.WriteString("user");
        writer.WriteString(value.Username);
        writer.WriteString("pass");
        writer.WriteString(value.Password);
    }
}
