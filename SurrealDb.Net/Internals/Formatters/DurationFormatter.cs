using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Formatters;

internal static class DurationFormatter
{
    public static (long seconds, int nanos) Convert(Duration value)
    {
        long seconds =
            value.Years * TimeConstants.SECONDS_PER_YEAR
            + value.Weeks * TimeConstants.SECONDS_PER_WEEK
            + value.Days * TimeConstants.SECONDS_PER_DAY
            + value.Hours * TimeConstants.SECONDS_PER_HOUR
            + value.Minutes * TimeConstants.SECONDS_PER_MINUTE
            + value.Seconds;

        int nanos =
            value.NanoSeconds
            + (value.MicroSeconds * TimeConstants.NANOS_PER_MICROSECOND)
            + (value.MilliSeconds * TimeConstants.NANOS_PER_MILLISECOND);

        return (seconds, nanos);
    }
}
