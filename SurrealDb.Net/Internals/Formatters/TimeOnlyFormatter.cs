#if NET6_0_OR_GREATER
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal static class TimeOnlyFormatter
{
    public static (long seconds, int nanos) Convert(TimeOnly value)
    {
        long seconds =
            value.Hour * TimeConstants.SECONDS_PER_HOUR
            + value.Minute * TimeConstants.SECONDS_PER_MINUTE
            + value.Second;

#if NET7_0_OR_GREATER
        int nanos =
            value.Nanosecond
            + (value.Microsecond * TimeConstants.NANOS_PER_MICROSECOND)
            + (value.Millisecond * TimeConstants.NANOS_PER_MILLISECOND);
#else
        var timeSpan = value.ToTimeSpan();

        double fractionedMilliseconds =
            timeSpan.TotalMilliseconds - Math.Truncate(timeSpan.TotalMilliseconds);
        int nanos =
            (int)(fractionedMilliseconds * TimeConstants.NANOS_PER_MILLISECOND)
            + (timeSpan.Milliseconds * TimeConstants.NANOS_PER_MILLISECOND);
#endif

        return (seconds, nanos);
    }
}
#endif
