using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;
#if NET5_0_OR_GREATER
using Pidgin;
#else
using Superpower;
#endif

namespace SurrealDb.Net.Internals.Parsers;

internal static partial class TimeSpanParser
{
    public static TimeSpan Convert(long? seconds, int? nanos)
    {
        return (seconds, nanos) switch
        {
            (null or 0, null or 0) => TimeSpan.Zero,
            (_, null or 0) => TimeSpan.FromSeconds((double)seconds),
            _
                => TimeSpan
                    .FromSeconds((double)seconds!)
                    .Add(TimeSpan.FromTicks((long)nanos / TimeConstants.NanosecondsPerTick))
        };
    }
}

#if NET5_0_OR_GREATER
internal static partial class TimeSpanParser
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

    private static readonly Parser<char, TimeSpan> DurationAsTimeSpanRaw =
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
internal static partial class TimeSpanParser
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

    private static readonly TextParser<TimeSpan> DurationAsTimeSpanRaw =
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
