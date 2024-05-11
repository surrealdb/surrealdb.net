using System.Text;
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
        await CborSerializer.SerializeAsync(value, stream, SurrealDbCborOptions.Default);

        var bytes = stream.ToArray();
        return ByteArrayToString(bytes);
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
        return CborSerializer.DeserializeAsync<T>(stream, SurrealDbCborOptions.Default);
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
