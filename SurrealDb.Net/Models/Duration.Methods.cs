using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Models;

public readonly partial struct Duration
{
    /// <summary>
    /// Converts the <see cref="Duration"/> to a <see cref="TimeSpan"/> unit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="OverflowException"></exception>
    public TimeSpan ToTimeSpan()
    {
        const int DAYS_PER_WEEK = 7;
        const int DAYS_PER_YEAR = 365;
        int days = Days + DAYS_PER_WEEK * Weeks + DAYS_PER_YEAR * Years;

        var timeSpanPartFromMilliseconds = new TimeSpan(
            days,
            Hours,
            Minutes,
            Seconds,
            MilliSeconds
        );

        long ticks =
            (MicroSeconds * TimeConstants.TicksPerMicrosecond)
            + (long)(NanoSeconds * TimeConstants.TicksPerNanosecond);

        var timeSpanPartFromNanoseconds = new TimeSpan(ticks);

        return timeSpanPartFromMilliseconds.Add(timeSpanPartFromNanoseconds);
    }
}
