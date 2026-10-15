using System.Text;
using System.Text.Json;

namespace SurrealDb.Net.Internals.Helpers;

internal static class JwtHelper
{
    /// <summary>
    /// Reads the expiry (<c>exp</c> claim) of a JWT, without validating it.
    /// </summary>
    public static bool TryGetExpiry(string jwt, out DateTimeOffset expiry)
    {
        expiry = default;

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(
                Encoding.UTF8.GetString(Convert.FromBase64String(payload))
            );

            if (
                document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("exp", out var exp)
                && exp.ValueKind == JsonValueKind.Number
                && exp.TryGetInt64(out long seconds)
            )
            {
                expiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
                return true;
            }
        }
        catch (FormatException) { }
        catch (JsonException) { }

        return false;
    }

    /// <summary>
    /// Indicates if the JWT expires within the given margin. A JWT without expiry never expires.
    /// </summary>
    public static bool ExpiresWithin(string jwt, TimeSpan margin)
    {
        return TryGetExpiry(jwt, out var expiry) && expiry - DateTimeOffset.UtcNow <= margin;
    }
}
