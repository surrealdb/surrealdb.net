using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal static class DateTimeFormatter
{
    public static (long seconds, int nanos) Convert(DateTime value)
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
