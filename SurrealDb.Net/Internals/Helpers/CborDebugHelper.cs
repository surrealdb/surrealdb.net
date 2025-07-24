using System.Text;

namespace SurrealDb.Net.Internals.Helpers;

internal static class CborDebugHelper
{
    public static string CborBinaryToHexa(System.IO.Stream stream)
    {
        long previousPosition = stream.Position;

        int index = 0;
        var stringBuilder = new StringBuilder();

        stream.Position = 0;

        while (index < stream.Length)
        {
            int b = stream.ReadByte();
            stringBuilder.AppendFormat("{0:x2}", b);
            index++;
        }

        stream.Position = previousPosition;

        return stringBuilder.ToString();
    }

    public static string CborBinaryToHexa(ReadOnlySpan<byte> bytes)
    {
        var stringBuilder = new StringBuilder(bytes.Length * 2);

        foreach (byte b in bytes)
        {
            stringBuilder.AppendFormat("{0:x2}", b);
        }

        return stringBuilder.ToString();
    }
}
