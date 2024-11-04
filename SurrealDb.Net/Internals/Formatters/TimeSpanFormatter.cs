using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal static class TimeSpanFormatter
{
    public static (long seconds, int nanos) Convert(TimeSpan value)
    {
        long seconds = (long)value.TotalSeconds;

#if NET7_0_OR_GREATER
        int nanos =
            value.Nanoseconds
            + (value.Microseconds * TimeConstants.NANOS_PER_MICROSECOND)
            + (value.Milliseconds * TimeConstants.NANOS_PER_MILLISECOND);
#else
        double fractionedMilliseconds =
            value.TotalMilliseconds - Math.Truncate(value.TotalMilliseconds);
        int nanos =
            (int)(fractionedMilliseconds * TimeConstants.NANOS_PER_MILLISECOND)
            + (value.Milliseconds * TimeConstants.NANOS_PER_MILLISECOND);
#endif

        return (seconds, nanos);
    }
}
