using System.Text;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Formatters;

internal static class TimeSpanFormatter
{
    public static string Format(TimeSpan value)
    {
        long seconds = (long)value.TotalSeconds;
        long nanoSeconds =
            (value.Ticks % TimeSpan.TicksPerSecond) * TimeConstants.NanosecondsPerTick;

        if (seconds == 0 && nanoSeconds == 0)
        {
            return "0ns";
        }

        long years = seconds / TimeConstants.SECONDS_PER_YEAR;
        seconds %= TimeConstants.SECONDS_PER_YEAR;

        long weeks = seconds / TimeConstants.SECONDS_PER_WEEK;
        seconds %= TimeConstants.SECONDS_PER_WEEK;

        long days = seconds / TimeConstants.SECONDS_PER_DAY;
        seconds %= TimeConstants.SECONDS_PER_DAY;

        long hours = seconds / TimeConstants.SECONDS_PER_HOUR;
        seconds %= TimeConstants.SECONDS_PER_HOUR;

        long minutes = seconds / TimeConstants.SECONDS_PER_MINUTE;
        seconds %= TimeConstants.SECONDS_PER_MINUTE;

        long milliSeconds = nanoSeconds / TimeConstants.NANOS_PER_MILLISECOND;
        nanoSeconds %= TimeConstants.NANOS_PER_MILLISECOND;

        long microSeconds = nanoSeconds / TimeConstants.NANOS_PER_MICROSECOND;
        nanoSeconds %= TimeConstants.NANOS_PER_MICROSECOND;

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
