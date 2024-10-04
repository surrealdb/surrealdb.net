using SurrealDb.Net.Internals.Constants;

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
