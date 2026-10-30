using System.Globalization;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Parsers;

internal static class DateTimeParser
{
    public static DateTime Convert(long seconds, int nanos)
    {
        return DateTime
            .UnixEpoch.AddSeconds(seconds)
            .AddTicks((long)Math.Round((double)nanos / TimeConstants.NanosecondsPerTick));
    }

    /// <summary>
    /// Parses a datetime sent as an ISO 8601 string (CBOR tag 0), as a UTC <see cref="DateTime"/>.
    /// </summary>
    public static DateTime Parse(string value)
    {
        return DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
        );
    }
}
