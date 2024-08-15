namespace SurrealDb.Net.Internals.Constants;

internal static class TimeConstants
{
    public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
    public const double TicksPerNanosecond = TicksPerMicrosecond / 1000d;
    public const long NanosecondsPerTick = (long)(1 / TicksPerNanosecond);

    public const int SECONDS_PER_MINUTE = 60;
    public const int SECONDS_PER_HOUR = 60 * SECONDS_PER_MINUTE;
    public const int SECONDS_PER_DAY = 24 * SECONDS_PER_HOUR;
    public const int SECONDS_PER_WEEK = 7 * SECONDS_PER_DAY;
    public const int SECONDS_PER_YEAR = 365 * SECONDS_PER_DAY;

    public const int NANOS_PER_SECOND = 1_000_000_000;
    public const int NANOS_PER_MILLISECOND = NANOS_PER_SECOND / 1000;
    public const int NANOS_PER_MICROSECOND = NANOS_PER_MILLISECOND / 1000;
}
