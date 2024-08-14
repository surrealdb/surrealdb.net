namespace SurrealDb.Net.Internals.Extensions;

internal static class ReadOnlyMemoryExtensions
{
    private static readonly byte[] _cborNone = [0xc6, 0xf6];
    private static readonly byte[] _cborEmptyArray = [0x80];

    public static bool ExpectNone(this ReadOnlySpan<byte> span)
    {
        return span.SequenceEqual(_cborNone);
    }

    public static bool ExpectEmptyArray(this ReadOnlySpan<byte> span)
    {
        return span.SequenceEqual(_cborEmptyArray);
    }
}
