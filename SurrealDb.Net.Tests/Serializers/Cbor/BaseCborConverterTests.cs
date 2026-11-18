using System.Buffers;
using System.Text;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

/// <summary>
/// Use https://cbor.nemo157.com/ to visualise the CBOR binary data, from HEXA to cbor-diag notation.
/// </summary>
public abstract class BaseCborConverterTests
{
    protected async Task<string> SerializeCborBinaryAsHexaAsync<T>(T value)
    {
        using var stream = new MemoryStream();
        await CborSerializer.SerializeAsync(value, stream, SurrealDbCborOptions.Default.Value);

        var bytes = stream.ToArray();
        return ByteArrayToString(bytes);
    }

    /// <summary>
    /// Serializes with the converter registered in the default SurrealDB options.
    /// </summary>
    protected static string SerializeAsHexa<T>(T value)
    {
        var options = SurrealDbCborOptions.Default.Value;
        return SerializeAsHexa(value, options.Registry.ConverterRegistry.Lookup<T>());
    }

    protected static string SerializeAsHexa<T>(T value, ICborConverter<T> converter)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new CborWriter(buffer);
        converter.Write(ref writer, value);

        return ByteArrayToString(buffer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Serializes with the reflection-based object converter of Dahomey.Cbor,
    /// as used when no converter is registered for <typeparamref name="T"/>.
    /// </summary>
    protected static string SerializeWithReflectionAsHexa<T>(T value)
    {
        var converter = new ObjectConverter<T>(SurrealDbCborOptions.Default.Value);
        return SerializeAsHexa(value, converter);
    }

    private static string ByteArrayToString(byte[] bytes)
    {
        var stringBuilder = new StringBuilder(bytes.Length * 2);

        foreach (byte b in bytes)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }

    protected ValueTask<T> DeserializeCborBinaryAsHexaAsync<T>(string binaryAsHexa)
    {
        var bytes = StringToByteArray(binaryAsHexa);
        using var stream = new MemoryStream(bytes);
        return CborSerializer.DeserializeAsync<T>(stream, SurrealDbCborOptions.Default.Value);
    }

    public static byte[] StringToByteArray(string binaryAsHexa)
    {
        var bytes = new byte[binaryAsHexa.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(binaryAsHexa.Substring(i * 2, 2), 16);
        }

        return bytes;
    }
}
