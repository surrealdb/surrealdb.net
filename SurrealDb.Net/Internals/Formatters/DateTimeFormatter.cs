using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal static class DateTimeFormatter
{
    public static (long seconds, int nanos) Convert(DateTime value)
    {
        var utcValue = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        var diff = utcValue - DateTime.UnixEpoch;

        long seconds = diff.Ticks / TimeSpan.TicksPerSecond;

#if NET7_0_OR_GREATER
        int nanos =
            utcValue.Nanosecond
            + (utcValue.Microsecond * TimeConstants.NANOS_PER_MICROSECOND)
            + (utcValue.Millisecond * TimeConstants.NANOS_PER_MILLISECOND);
#else
        double fractionedMilliseconds =
            utcValue.TimeOfDay.TotalMilliseconds
            - Math.Truncate(utcValue.TimeOfDay.TotalMilliseconds);
        int nanos =
            (int)(fractionedMilliseconds * TimeConstants.NANOS_PER_MILLISECOND)
            + (utcValue.Millisecond * TimeConstants.NANOS_PER_MILLISECOND);
#endif

        if (nanos > 0 && seconds < 0)
        {
            // 💡 We need take into account the nanos part when the number of seconds is negative (before 01/01/1970)
            seconds--;
        }

        return (seconds, nanos);
    }

    public static (long seconds, int nanos) Convert(DateTimeOffset value)
    {
        var diff = value - DateTime.UnixEpoch;

        long seconds = (long)diff.TotalSeconds;

#if NET7_0_OR_GREATER
        int nanos =
            value.Nanosecond
            + (value.Microsecond * TimeConstants.NANOS_PER_MICROSECOND)
            + (value.Millisecond * TimeConstants.NANOS_PER_MILLISECOND);
#else
        double fractionedMilliseconds =
            value.TimeOfDay.TotalMilliseconds - Math.Truncate(value.TimeOfDay.TotalMilliseconds);
        int nanos =
            (int)(fractionedMilliseconds * TimeConstants.NANOS_PER_MILLISECOND)
            + (value.Millisecond * TimeConstants.NANOS_PER_MILLISECOND);
#endif

        return (seconds, nanos);
    }
}
