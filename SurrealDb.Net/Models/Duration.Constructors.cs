using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Internals.Parsers;

namespace SurrealDb.Net.Models;

public readonly partial struct Duration
{
    /// <summary>
    /// Creates a default <see cref="Duration"/>, equivalent to "0ns".
    /// </summary>
    public Duration() { }

    internal Duration(string value)
    {
        var unitValues = DurationParser
            .Parse(value)
            .Where(kv => kv.value != 0)
            .ToDictionary(kv => kv.unit, kv => (int)kv.value);

        NanoSeconds = unitValues.GetValueOrDefault(DurationUnit.NanoSecond);
        MicroSeconds = unitValues.GetValueOrDefault(DurationUnit.MicroSecond);
        MilliSeconds = unitValues.GetValueOrDefault(DurationUnit.MilliSecond);
        Seconds = unitValues.GetValueOrDefault(DurationUnit.Second);
        Minutes = unitValues.GetValueOrDefault(DurationUnit.Minute);
        Hours = unitValues.GetValueOrDefault(DurationUnit.Hour);
        Days = unitValues.GetValueOrDefault(DurationUnit.Day);
        Weeks = unitValues.GetValueOrDefault(DurationUnit.Week);
        Years = unitValues.GetValueOrDefault(DurationUnit.Year);
    }

    /// <summary>
    /// Creates a <see cref="Duration"/> from <paramref name="seconds"/> and <paramref name="nanoseconds"/> parts.
    /// </summary>
    /// <param name="seconds">The total number of seconds to store in this <see cref="Duration"/>. Defaults to 0.</param>
    /// <param name="nanoseconds">The total number of nanoseconds to store in this <see cref="Duration"/>. Defaults to 0.</param>
    public Duration(long seconds = 0, int nanoseconds = 0)
    {
        if (seconds != 0)
        {
            long remainingSeconds = seconds;

            Years = (int)(remainingSeconds / TimeConstants.SECONDS_PER_YEAR);
            if (Years != 0)
            {
                remainingSeconds -= Years * TimeConstants.SECONDS_PER_YEAR;
            }

            Weeks = (int)(remainingSeconds / TimeConstants.SECONDS_PER_WEEK);
            if (Weeks != 0)
            {
                remainingSeconds -= Weeks * TimeConstants.SECONDS_PER_WEEK;
            }

            Days = (int)(remainingSeconds / TimeConstants.SECONDS_PER_DAY);
            if (Days != 0)
            {
                remainingSeconds -= Days * TimeConstants.SECONDS_PER_DAY;
            }

            Hours = (int)(remainingSeconds / TimeConstants.SECONDS_PER_HOUR);
            if (Hours != 0)
            {
                remainingSeconds -= Hours * TimeConstants.SECONDS_PER_HOUR;
            }

            Minutes = (int)(remainingSeconds / TimeConstants.SECONDS_PER_MINUTE);
            if (Minutes != 0)
            {
                remainingSeconds -= Minutes * TimeConstants.SECONDS_PER_MINUTE;
            }

            if (remainingSeconds != 0)
            {
                Seconds = (int)remainingSeconds;
            }
        }

        if (nanoseconds != 0)
        {
            int remainingNanos = nanoseconds;

            MilliSeconds = remainingNanos / TimeConstants.NANOS_PER_MILLISECOND;
            if (MilliSeconds != 0)
            {
                remainingNanos -= MilliSeconds * TimeConstants.NANOS_PER_MILLISECOND;
            }

            MicroSeconds = remainingNanos / TimeConstants.NANOS_PER_MICROSECOND;
            if (MicroSeconds != 0)
            {
                remainingNanos -= MicroSeconds * TimeConstants.NANOS_PER_MICROSECOND;
            }

            if (remainingNanos != 0)
            {
                NanoSeconds = remainingNanos;
            }
        }
    }
}
