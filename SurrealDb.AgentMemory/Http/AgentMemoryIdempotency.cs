using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Idempotency-key derivation for safe write retries.
/// </summary>
/// <remarks>
/// The key is a SHA-256 digest of the request method, path, body, and a 30-second time bucket,
/// the same derivation the JavaScript and Python clients use.
/// Identical writes replayed within the same bucket collapse to a single server-side effect,
/// which makes the <c>/facts</c> and <c>/facts/batch</c> writes safe to retry.
/// </remarks>
public static class AgentMemoryIdempotency
{
    private const long BucketSeconds = 30;

    /// <summary>
    /// Computes an idempotency key for a write request.
    /// </summary>
    /// <param name="method">HTTP method, for example <c>POST</c>.</param>
    /// <param name="path">Request path including the context prefix, without the query string.</param>
    /// <param name="body">Serialized request body, or an empty string when there is none.</param>
    /// <param name="now">The current time.</param>
    /// <returns>A lower-case, hex-encoded SHA-256 digest (64 characters).</returns>
    public static string ComputeKey(string method, string path, string body, DateTimeOffset now)
    {
        var bucket = now.ToUnixTimeMilliseconds() / 1000 / BucketSeconds;
        var material = $"{method}\0{path}\0{body}\0{bucket.ToString(CultureInfo.InvariantCulture)}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexStringLower(digest);
    }
}
