namespace SurrealDb.Net.Internals.Parsers;

internal static class TimeSpanParser
{
    public static TimeSpan Convert(long? seconds, int? nanos)
    {
        return (seconds, nanos) switch
        {
            (null or 0, null or 0) => TimeSpan.Zero,
            (_, null or 0) => TimeSpan.FromSeconds((double)seconds),
            _ => TimeSpan.FromTicks(
                (seconds!.Value * TimeSpan.TicksPerSecond) + (nanos!.Value / 100)
            ),
        };
    }
}
