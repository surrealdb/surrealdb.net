namespace SurrealDb.Net.Internals.Extensions;

internal static class ReadOnlySpanExtensions
{
    public static ReadOnlyMemory<byte> ToMemory(this ref ReadOnlySpan<byte> span)
    {
        var buffer = new byte[span.Length];
        span.CopyTo(buffer);
        return new ReadOnlyMemory<byte>(buffer);
    }

    public static ReadOnlyMemory<byte> ToMemoryNonRef(this ReadOnlySpan<byte> span)
    {
        var buffer = new byte[span.Length];
        span.CopyTo(buffer);
        return new ReadOnlyMemory<byte>(buffer);
    }
}
