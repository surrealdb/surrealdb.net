#if NET6_0_OR_GREATER
namespace SurrealDb.Net.Internals.Parsers;

internal static class TimeOnlyParser
{
    public static TimeOnly Convert(long? seconds, int? nanos)
    {
        return Convert(TimeSpanParser.Convert(seconds, nanos));
    }

    public static TimeOnly Convert(TimeSpan timeSpan)
    {
        return new TimeOnly(timeSpan.Ticks % TimeSpan.TicksPerDay);
    }
}
#endif
