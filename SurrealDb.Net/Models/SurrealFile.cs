using System.Text;

namespace SurrealDb.Net.Models;

/// <summary>
/// This represents the SurrealDB <c>File</c> type. Some examples of file pointers:
/// <list type="bullet">
/// <item>
/// "bucket:/some/key/to/a/file.txt"
/// </item>
/// <item>
/// "bucket:/some/key/with\ escaped"
/// </item>
/// </list>
/// </summary>
public sealed record SurrealFile
{
    private const string DefaultBucket = "file";

    /// <summary>
    /// The bucket name.
    /// </summary>
    public string Bucket { get; } = DefaultBucket;

    /// <summary>
    /// The file path.
    /// <remarks>The path always starts with a "/".</remarks>
    /// </summary>
    public string Path { get; } = "/";

    /// <summary>
    /// Creates a new <see cref="SurrealFile"/> with the default bucket and empty file path.
    /// </summary>
    public SurrealFile()
    {
        Bucket = DefaultBucket;
    }

    /// <summary>
    /// Creates a new <see cref="SurrealFile"/> with the default bucket and a file path.
    /// </summary>
    /// <param name="path">The expected path of the file.</param>
    public SurrealFile(string path)
    {
        Path = EnsuresPathStartsWithSlash(path);
    }

    /// <summary>
    /// Creates a new <see cref="SurrealFile"/> with a bucket and a file path.
    /// </summary>
    /// <param name="bucket">The expected bucket that should contain the file.</param>
    /// <param name="path">The expected path of the file.</param>
    public SurrealFile(string bucket, string path)
    {
        Bucket = bucket;
        Path = EnsuresPathStartsWithSlash(path);
    }

    private static string EnsuresPathStartsWithSlash(string path)
    {
        return path.StartsWith('/') ? path : $"/{path}";
    }

    public override string ToString()
    {
        const char separator = ':';

        var stringBuilder = new StringBuilder(Bucket.Length + 1 + Path.Length);

        foreach (var c in Bucket)
        {
            if (!IsAllowed(c, true))
            {
                stringBuilder.Append('\\');
            }
            stringBuilder.Append(c);
        }

        stringBuilder.Append(separator);

        foreach (var c in Path)
        {
            if (!IsAllowed(c, false))
            {
                stringBuilder.Append('\\');
            }
            stringBuilder.Append(c);
        }

        return stringBuilder.ToString();
    }

    private static bool IsAllowed(char c, bool escapeSlash)
    {
        return IsAsciiLetterOrDigit(c)
            || c == '-'
            || c == '_'
            || c == '.'
            || (!escapeSlash && c == '/');
    }

    private static bool IsAsciiLetterOrDigit(char c)
    {
#if NET7_0_OR_GREATER
        return char.IsAsciiLetterOrDigit(c);
#else
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
#endif
    }
}
