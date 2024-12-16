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

    public static DateTimeOffset ConvertOffset(long seconds, int nanos)
    {
        return DateTimeOffset
            .UnixEpoch.AddSeconds(seconds)
            .AddTicks((long)Math.Round((double)nanos / TimeConstants.NanosecondsPerTick));
    }
}
