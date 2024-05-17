using SurrealDb.Net.LocalBenchmarks.Models;

namespace SurrealDb.Net.LocalBenchmarks.Parsers;

internal static class FromArrayDurationParser
{
    private const int SECONDS_PER_MINUTE = 60;
    private const int SECONDS_PER_HOUR = 60 * SECONDS_PER_MINUTE;
    private const int SECONDS_PER_DAY = 24 * SECONDS_PER_HOUR;
    private const int SECONDS_PER_WEEK = 7 * SECONDS_PER_DAY;
    private const int SECONDS_PER_YEAR = 52 * SECONDS_PER_WEEK;

    private const int NANOS_PER_SECOND = 1_000_000_000;
    private const int NANOS_PER_MILLISECOND = NANOS_PER_SECOND / 1000;
    private const int NANOS_PER_MICROSECOND = NANOS_PER_MILLISECOND / 1000;

    public static Dictionary<DurationUnit, int> Parse(long? seconds, int? nanos)
    {
        var value = new Dictionary<DurationUnit, int>();

        if (seconds.HasValue)
        {
            long remainingSeconds = seconds.Value;

            int years = (int)(remainingSeconds / SECONDS_PER_YEAR);
            if (years != 0)
            {
                remainingSeconds -= years * SECONDS_PER_YEAR;
                value.Add(DurationUnit.Year, years);
            }

            int weeks = (int)(remainingSeconds / SECONDS_PER_WEEK);
            if (weeks != 0)
            {
                remainingSeconds -= weeks * SECONDS_PER_WEEK;
                value.Add(DurationUnit.Week, weeks);
            }

            int days = (int)(remainingSeconds / SECONDS_PER_DAY);
            if (days != 0)
            {
                remainingSeconds -= days * SECONDS_PER_DAY;
                value.Add(DurationUnit.Day, days);
            }

            int hours = (int)(remainingSeconds / SECONDS_PER_HOUR);
            if (hours != 0)
            {
                remainingSeconds -= hours * SECONDS_PER_HOUR;
                value.Add(DurationUnit.Hour, hours);
            }

            int minutes = (int)(remainingSeconds / SECONDS_PER_MINUTE);
            if (minutes != 0)
            {
                remainingSeconds -= minutes * SECONDS_PER_MINUTE;
                value.Add(DurationUnit.Minute, minutes);
            }

            if (remainingSeconds != 0)
            {
                value.Add(DurationUnit.Second, (int)remainingSeconds);
            }
        }

        if (nanos.HasValue)
        {
            int remainingNanos = nanos.Value;

            int millis = remainingNanos / NANOS_PER_MILLISECOND;
            if (millis != 0)
            {
                remainingNanos -= millis * NANOS_PER_MILLISECOND;
                value.Add(DurationUnit.MilliSecond, millis);
            }

            int micros = remainingNanos / NANOS_PER_MICROSECOND;
            if (micros != 0)
            {
                remainingNanos -= micros * NANOS_PER_MICROSECOND;
                value.Add(DurationUnit.MicroSecond, micros);
            }

            if (remainingNanos != 0)
            {
                value.Add(DurationUnit.NanoSecond, remainingNanos);
            }
        }

        return value;
    }
}
