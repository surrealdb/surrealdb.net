#if NET5_0_OR_GREATER
using Pidgin;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals.Parsers;

internal static class TimeSpanParser
{
    private static TimeSpan ToTimeSpan(double value, DurationUnit unit)
    {
        return unit switch
        {
            DurationUnit.NanoSecond
                => TimeSpan.FromTicks((long)Math.Round(value * TimeConstants.TicksPerNanosecond)),
            DurationUnit.MicroSecond
                => TimeSpan.FromTicks((long)(value * TimeConstants.TicksPerMicrosecond)),
            DurationUnit.MilliSecond => TimeSpan.FromMilliseconds(value),
            DurationUnit.Second => TimeSpan.FromSeconds(value),
            DurationUnit.Minute => TimeSpan.FromMinutes(value),
            DurationUnit.Hour => TimeSpan.FromHours(value),
            DurationUnit.Day => TimeSpan.FromDays(value),
            DurationUnit.Week => TimeSpan.FromDays(value * 7),
            DurationUnit.Year => TimeSpan.FromDays(value * 365),
            _ => throw new ArgumentException($"Invalid duration unit: {unit}")
        };
    }

    public static readonly Parser<char, TimeSpan> DurationAsTimeSpanRaw =
        from pair in DurationParser.DurationRaw
        select ToTimeSpan(pair.value, pair.unit);

    private static readonly Parser<char, IEnumerable<TimeSpan>> DurationAsTimeSpanParser =
        DurationAsTimeSpanRaw.AtLeastOnce();

    public static TimeSpan Parse(string input)
    {
        var result = DurationAsTimeSpanParser.ParseOrThrow(input);
        return result.Aggregate(TimeSpan.Zero, (acc, span) => acc + span);
    }
}
#else
using Superpower;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals.Parsers;

internal static class TimeSpanParser
{
    private static TimeSpan ToTimeSpan(double value, DurationUnit unit)
    {
        return unit switch
        {
            DurationUnit.NanoSecond
                => TimeSpan.FromTicks((long)Math.Round(value * TimeConstants.TicksPerNanosecond)),
            DurationUnit.MicroSecond
                => TimeSpan.FromTicks((long)(value * TimeConstants.TicksPerMicrosecond)),
            DurationUnit.MilliSecond => TimeSpan.FromMilliseconds(value),
            DurationUnit.Second => TimeSpan.FromSeconds(value),
            DurationUnit.Minute => TimeSpan.FromMinutes(value),
            DurationUnit.Hour => TimeSpan.FromHours(value),
            DurationUnit.Day => TimeSpan.FromDays(value),
            DurationUnit.Week => TimeSpan.FromDays(value * 7),
            DurationUnit.Year => TimeSpan.FromDays(value * 365),
            _ => throw new ArgumentException($"Invalid duration unit: {unit}")
        };
    }

    public static readonly TextParser<TimeSpan> DurationAsTimeSpanRaw =
        from pair in DurationParser.DurationRaw
        select ToTimeSpan(pair.value, pair.unit);

    private static readonly TextParser<TimeSpan[]> DurationAsTimeSpanParser =
        DurationAsTimeSpanRaw.AtLeastOnce();

    public static TimeSpan Parse(string input)
    {
        var result = DurationAsTimeSpanParser.Parse(input);
        return result.Aggregate(TimeSpan.Zero, (acc, span) => acc + span);
    }
}
#endif
