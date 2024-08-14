namespace SurrealDb.Net.Internals.Helpers;

internal static class RandomHelper
{
#if NET6_0_OR_GREATER
    private static Random _random => Random.Shared;
#else
    private static readonly Random _random = new();
    private static readonly object _randomLock = new();
#endif

    private const string _encodeString = "0123456789abcdef";
    private static readonly int _encodeMaxIndex = _encodeString.Length - 1;
    private static readonly char[] _encode32Chars = _encodeString.ToCharArray();

    /// <summary>
    /// Generates a random string with 8 characters
    /// </summary>
    public static string CreateRandomId()
    {
#if NET6_0_OR_GREATER
        int randomNumber = _random.Next();
#else
        int randomNumber;
        lock (_randomLock)
        {
            randomNumber = _random.Next();
        }
#endif
        const int length = 8;

        return string.Create(
            length,
            randomNumber,
            (buffer, value) =>
            {
                buffer[7] = _encode32Chars[value & _encodeMaxIndex];
                buffer[6] = _encode32Chars[(value >> 5) & _encodeMaxIndex];
                buffer[5] = _encode32Chars[(value >> 10) & _encodeMaxIndex];
                buffer[4] = _encode32Chars[(value >> 15) & _encodeMaxIndex];
                buffer[3] = _encode32Chars[(value >> 20) & _encodeMaxIndex];
                buffer[2] = _encode32Chars[(value >> 25) & _encodeMaxIndex];
                buffer[1] = _encode32Chars[(value >> 30) & _encodeMaxIndex];
                buffer[0] = _encode32Chars[(value >> 35) & _encodeMaxIndex];
            }
        );
    }
}
