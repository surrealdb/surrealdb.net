using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;
#if NET5_0_OR_GREATER
using Pidgin;
using static Pidgin.Parser;
#else
using Superpower;
using Superpower.Parsers;
#endif

namespace SurrealDb.Net.Internals.Parsers;

internal static partial class DurationParser
{
    public static Dictionary<DurationUnit, int> Convert(long? seconds, int? nanos)
    {
        var value = new Dictionary<DurationUnit, int>();

        if (seconds.HasValue)
        {
            long remainingSeconds = seconds.Value;

            int years = (int)(remainingSeconds / TimeConstants.SECONDS_PER_YEAR);
            if (years != 0)
            {
                remainingSeconds -= years * TimeConstants.SECONDS_PER_YEAR;
                value.Add(DurationUnit.Year, years);
            }

            int weeks = (int)(remainingSeconds / TimeConstants.SECONDS_PER_WEEK);
            if (weeks != 0)
            {
                remainingSeconds -= weeks * TimeConstants.SECONDS_PER_WEEK;
                value.Add(DurationUnit.Week, weeks);
            }

            int days = (int)(remainingSeconds / TimeConstants.SECONDS_PER_DAY);
            if (days != 0)
            {
                remainingSeconds -= days * TimeConstants.SECONDS_PER_DAY;
                value.Add(DurationUnit.Day, days);
            }

            int hours = (int)(remainingSeconds / TimeConstants.SECONDS_PER_HOUR);
            if (hours != 0)
            {
                remainingSeconds -= hours * TimeConstants.SECONDS_PER_HOUR;
                value.Add(DurationUnit.Hour, hours);
            }

            int minutes = (int)(remainingSeconds / TimeConstants.SECONDS_PER_MINUTE);
            if (minutes != 0)
            {
                remainingSeconds -= minutes * TimeConstants.SECONDS_PER_MINUTE;
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

            int millis = remainingNanos / TimeConstants.NANOS_PER_MILLISECOND;
            if (millis != 0)
            {
                remainingNanos -= millis * TimeConstants.NANOS_PER_MILLISECOND;
                value.Add(DurationUnit.MilliSecond, millis);
            }

            int micros = remainingNanos / TimeConstants.NANOS_PER_MICROSECOND;
            if (micros != 0)
            {
                remainingNanos -= micros * TimeConstants.NANOS_PER_MICROSECOND;
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

#if NET5_0_OR_GREATER
internal static partial class DurationParser
{
    private static readonly Parser<char, DurationUnit> DurationUnitParser = Try(
            String("ns").Map(_ => DurationUnit.NanoSecond)
        )
        .Or(Try(String("µs").Or(String("us")).Map(_ => DurationUnit.MicroSecond)))
        .Or(Try(String("ms").Map(_ => DurationUnit.MilliSecond)))
        .Or(Try(String("s").Map(_ => DurationUnit.Second)))
        .Or(Try(String("m").Map(_ => DurationUnit.Minute)))
        .Or(Try(String("h").Map(_ => DurationUnit.Hour)))
        .Or(Try(String("d").Map(_ => DurationUnit.Day)))
        .Or(Try(String("w").Map(_ => DurationUnit.Week)))
        .Or(Try(String("y").Map(_ => DurationUnit.Year)));

    public static readonly Parser<char, (double value, DurationUnit unit)> DurationRaw =
        from v in Real
        from u in DurationUnitParser!
        select (v, u);

    private static readonly Parser<char, IEnumerable<(double value, DurationUnit unit)>> Parser =
        DurationRaw.AtLeastOnce();

    public static IEnumerable<(double value, DurationUnit unit)> Parse(string input)
    {
        return Parser.ParseOrThrow(input);
    }
}
#else
internal static partial class DurationParser
{
    private static readonly TextParser<DurationUnit> DurationUnitParser = Span.EqualTo("ns")
        .Value(DurationUnit.NanoSecond)
        .Try()
        .Or(Span.EqualTo("µs").Value(DurationUnit.MicroSecond))
        .Try()
        .Or(Span.EqualTo("us").Value(DurationUnit.MicroSecond))
        .Try()
        .Or(Span.EqualTo("ms").Value(DurationUnit.MilliSecond))
        .Try()
        .Or(Span.EqualTo("s").Value(DurationUnit.Second))
        .Try()
        .Or(Span.EqualTo("m").Value(DurationUnit.Minute))
        .Try()
        .Or(Span.EqualTo("h").Value(DurationUnit.Hour))
        .Try()
        .Or(Span.EqualTo("d").Value(DurationUnit.Day))
        .Try()
        .Or(Span.EqualTo("w").Value(DurationUnit.Week))
        .Try()
        .Or(Span.EqualTo("y").Value(DurationUnit.Year));

    public static readonly TextParser<(double value, DurationUnit unit)> DurationRaw =
        from v in Numerics.DecimalDouble
        from u in DurationUnitParser!
        select (v, u);

    private static readonly TextParser<(double value, DurationUnit unit)[]> Parser =
        DurationRaw.AtLeastOnce();

    public static (double value, DurationUnit unit)[] Parse(string input)
    {
        return Parser.Parse(input);
    }
}
#endif
