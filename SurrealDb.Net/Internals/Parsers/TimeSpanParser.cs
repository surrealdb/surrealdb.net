using Superpower;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals.Parsers;

internal static class TimeSpanParser
{
    private static TimeSpan ToTimeSpan(decimal value, DurationUnit unit)
    {
        return unit switch
        {
            DurationUnit.NanoSecond => TimeSpan.FromTicks((long)Math.Round((double)value * TimeConstants.TicksPerNanosecond)),
            DurationUnit.MicroSecond => TimeSpan.FromTicks((long)(value * TimeConstants.TicksPerMicrosecond)),
            DurationUnit.MilliSecond => TimeSpan.FromMilliseconds((double)value),
            DurationUnit.Second => TimeSpan.FromSeconds((double)value),
            DurationUnit.Minute => TimeSpan.FromMinutes((double)value),
            DurationUnit.Hour => TimeSpan.FromHours((double)value),
            DurationUnit.Day => TimeSpan.FromDays((double)value),
            DurationUnit.Week => TimeSpan.FromDays((double)value * 7),
            DurationUnit.Year => TimeSpan.FromDays((double)value * 365),
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
