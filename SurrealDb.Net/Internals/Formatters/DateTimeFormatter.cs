using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal static class DateTimeFormatter
{
    public static (long seconds, int nanos) Convert(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

        var ticks = utc.Ticks - DateTime.UnixEpoch.Ticks;

        var seconds = ticks / TimeSpan.TicksPerSecond;
        var remaining = ticks % TimeSpan.TicksPerSecond;
        var nanos = (int)remaining * 100;

        return (seconds, nanos);
    }
}
