using Superpower.Parsers;
using Superpower;
using SurrealDb.Internals.Constants;

namespace SurrealDb.Internals.Parsers;

internal class TimeSpanParser
{
    private enum TimeUnit
    {
        NanoSecond,
        MicroSecond,
        MilliSecond,
        Second,
        Minute,
        Hour,
        Day,
        Week,
        Year
    }

    private static TimeSpan ToTimeSpan(decimal value, TimeUnit unit)
    {
        return unit switch
        {
            TimeUnit.NanoSecond => TimeSpan.FromTicks((long)Math.Round((double)value * TimeConstants.TicksPerNanosecond)),
            TimeUnit.MicroSecond => TimeSpan.FromTicks((long)(value * TimeConstants.TicksPerMicrosecond)),
            TimeUnit.MilliSecond => TimeSpan.FromMilliseconds((double)value),
            TimeUnit.Second => TimeSpan.FromSeconds((double)value),
            TimeUnit.Minute => TimeSpan.FromMinutes((double)value),
            TimeUnit.Hour => TimeSpan.FromHours((double)value),
            TimeUnit.Day => TimeSpan.FromDays((double)value),
            TimeUnit.Week => TimeSpan.FromDays((double)value * 7),
            TimeUnit.Year => TimeSpan.FromDays((double)value * 365),
            _ => throw new ArgumentException($"Invalid duration unit: {unit}")
        };
    }

    private static readonly TextParser<TimeUnit> TimeUnitParser =
        Span.EqualTo("ns").Value(TimeUnit.NanoSecond)
            .Try().Or(Span.EqualTo("µs").Value(TimeUnit.MicroSecond))
            .Try().Or(Span.EqualTo("us").Value(TimeUnit.MicroSecond))
            .Try().Or(Span.EqualTo("ms").Value(TimeUnit.MilliSecond))
            .Try().Or(Span.EqualTo("s").Value(TimeUnit.Second))
            .Try().Or(Span.EqualTo("m").Value(TimeUnit.Minute))
            .Try().Or(Span.EqualTo("h").Value(TimeUnit.Hour))
            .Try().Or(Span.EqualTo("d").Value(TimeUnit.Day))
            .Try().Or(Span.EqualTo("w").Value(TimeUnit.Week))
            .Try().Or(Span.EqualTo("y").Value(TimeUnit.Year));

    private static readonly TextParser<TimeSpan> DurationRaw =
        from v in Numerics.DecimalDecimal
        from u in TimeUnitParser!
        select ToTimeSpan(v, u);

    private static readonly TextParser<TimeSpan[]> DurationParser =
        DurationRaw.AtLeastOnce();

    public static TimeSpan Parse(string input)
    {
        var result = DurationParser.Parse(input);
        return result.Aggregate(TimeSpan.Zero, (acc, span) => acc + span);
    }
}
