namespace SurrealDb.Net.Internals.Constants;

internal class TimeConstants
{
    public const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
    public const double TicksPerNanosecond = TicksPerMicrosecond / 1000d;
    public const long NanosecondsPerTick = (long)(1 / TicksPerNanosecond);
}
