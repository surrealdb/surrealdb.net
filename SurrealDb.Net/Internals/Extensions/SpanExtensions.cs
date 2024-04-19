using System.Globalization;

namespace SurrealDb.Net.Internals.Extensions;

internal static class SpanExtensions
{
    public static void Write(this ref Span<char> buffer, ReadOnlySpan<char> value)
    {
        value.CopyTo(buffer);
        buffer = buffer[value.Length..];
    }

    public static void Write(this ref Span<char> buffer, char value)
    {
        buffer[0] = value;
        buffer = buffer[1..];
    }

    public static bool Write(
        this ref Span<char> buffer,
        int value,
        IFormatProvider? provider = null
    )
    {
        if (
            value.TryFormat(
                buffer,
                out int charsWritten,
                provider: provider ?? CultureInfo.InvariantCulture
            )
        )
        {
            buffer = buffer[charsWritten..];
            return true;
        }

        return false;
    }
}
