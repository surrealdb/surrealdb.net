using System.Text;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal class TimeSpanFormatter
{
    const long NANOSECONDS_PER_MICROSECOND = 1000;
    const long NANOSECONDS_PER_MILLISECOND = NANOSECONDS_PER_MICROSECOND * 1000;
    const long SECONDS_PER_MINUTE = 60;
    const long SECONDS_PER_HOUR = SECONDS_PER_MINUTE * 60;
    const long SECONDS_PER_DAY = SECONDS_PER_HOUR * 24;
    const long SECONDS_PER_WEEK = SECONDS_PER_DAY * 7;
    const long SECONDS_PER_YEAR = SECONDS_PER_DAY * 365;

    public static string Format(TimeSpan value)
    {
        long seconds = (long)value.TotalSeconds;
        long nanoSeconds =
            (value.Ticks % TimeSpan.TicksPerSecond) * TimeConstants.NanosecondsPerTick;

        if (seconds == 0 && nanoSeconds == 0)
        {
            return "0ns";
        }

        long years = seconds / SECONDS_PER_YEAR;
        seconds %= SECONDS_PER_YEAR;

        long weeks = seconds / SECONDS_PER_WEEK;
        seconds %= SECONDS_PER_WEEK;

        long days = seconds / SECONDS_PER_DAY;
        seconds %= SECONDS_PER_DAY;

        long hours = seconds / SECONDS_PER_HOUR;
        seconds %= SECONDS_PER_HOUR;

        long minutes = seconds / SECONDS_PER_MINUTE;
        seconds %= SECONDS_PER_MINUTE;

        long milliSeconds = nanoSeconds / NANOSECONDS_PER_MILLISECOND;
        nanoSeconds %= NANOSECONDS_PER_MILLISECOND;

        long microSeconds = nanoSeconds / NANOSECONDS_PER_MICROSECOND;
        nanoSeconds %= NANOSECONDS_PER_MICROSECOND;

        var formattedValueStringBuilder = new StringBuilder();

        if (years > 0)
            formattedValueStringBuilder.Append($"{years}y");
        if (weeks > 0)
            formattedValueStringBuilder.Append($"{weeks}w");
        if (days > 0)
            formattedValueStringBuilder.Append($"{days}d");
        if (hours > 0)
            formattedValueStringBuilder.Append($"{hours}h");
        if (minutes > 0)
            formattedValueStringBuilder.Append($"{minutes}m");
        if (seconds > 0)
            formattedValueStringBuilder.Append($"{seconds}s");
        if (milliSeconds > 0)
            formattedValueStringBuilder.Append($"{milliSeconds}ms");
        if (microSeconds > 0)
            formattedValueStringBuilder.Append($"{microSeconds}µs");
        if (nanoSeconds > 0)
            formattedValueStringBuilder.Append($"{nanoSeconds}ns");

        return formattedValueStringBuilder.ToString();
    }
}
