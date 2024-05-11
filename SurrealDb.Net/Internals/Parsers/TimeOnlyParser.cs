#if NET6_0_OR_GREATER
namespace SurrealDb.Net.Internals.Parsers;

internal static class TimeOnlyParser
{
    public static TimeOnly Parse(string input)
    {
        var timeSpan = TimeSpanParser.Parse(input);
        return new TimeOnly(timeSpan.Ticks % TimeSpan.TicksPerDay);
    }

    public static TimeOnly Convert(long? seconds, int? nanos)
    {
        var timeSpan = TimeSpanParser.Convert(seconds, nanos);
        return new TimeOnly(timeSpan.Ticks % TimeSpan.TicksPerDay);
    }
}
#endif
