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
        long ticksInSeconds = seconds * TimeSpan.TicksPerSecond;
        long remainingTicks = value.Ticks - ticksInSeconds;

        int nanos = (int)(remainingTicks * TimeConstants.NanosecondsPerTick);
#endif

        return (seconds, nanos);
    }
}
