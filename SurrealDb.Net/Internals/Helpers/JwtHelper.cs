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

        using var document = TryParsePayload(jwt);

        if (
            document is not null
            && document.RootElement.TryGetProperty("exp", out var exp)
            && exp.ValueKind == JsonValueKind.Number
            && exp.TryGetInt64(out long seconds)
        )
        {
            expiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }

        return false;
    }

    /// <summary>
    /// The claims of a JWT that name who it was issued to (namespace, database, access method and record),
    /// without validating it. A renewed token keeps them.
    /// </summary>
    public static bool TryGetSubject(string jwt, out string subject)
    {
        subject = string.Empty;

        using var document = TryParsePayload(jwt);
        if (document is null)
        {
            return false;
        }

        var builder = new StringBuilder();
        foreach (string claim in SubjectClaims)
        {
            builder.Append(claim).Append('=');
            if (document.RootElement.TryGetProperty(claim, out var value))
            {
                builder.Append(value.GetRawText());
            }
            builder.Append(';');
        }

        subject = builder.ToString();
        return true;
    }

    private static readonly string[] SubjectClaims = ["NS", "DB", "AC", "SC", "ID"];

    private static JsonDocument? TryParsePayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var document = JsonDocument.Parse(
                Encoding.UTF8.GetString(Convert.FromBase64String(payload))
            );

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return document;
            }

            document.Dispose();
        }
        catch (FormatException) { }
        catch (JsonException) { }

        return null;
    }

    /// <summary>
    /// Indicates if the JWT expires within the given margin. A JWT without expiry never expires.
    /// </summary>
    public static bool ExpiresWithin(string jwt, TimeSpan margin)
    {
        return TryGetExpiry(jwt, out var expiry) && expiry - DateTimeOffset.UtcNow <= margin;
    }
}
