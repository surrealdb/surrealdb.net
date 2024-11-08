namespace SurrealDb.Net.Internals.Parsers;

internal static partial class DateTimeParser
{
    public static DateTime Convert(long seconds, int nanos)
    {
        var ns = (long)Math.Round((float)nanos / 100);
        return DateTime.UnixEpoch.AddTicks((seconds * TimeSpan.TicksPerSecond) + ns);
    }
}
