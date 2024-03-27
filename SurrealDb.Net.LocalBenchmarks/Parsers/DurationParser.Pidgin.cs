using Pidgin;
using SurrealDb.Net.LocalBenchmarks.Models;
using static Pidgin.Parser;

namespace SurrealDb.Net.LocalBenchmarks.Parsers;

internal static class PidginDurationParser
{
    public static readonly Parser<char, DurationUnit> DurationUnitParser = Try(
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

    public static readonly Parser<char, (int value, DurationUnit unit)> DurationRaw =
        from v in DecimalNum
        from u in DurationUnitParser!
        select (v, u);

    public static readonly Parser<char, IEnumerable<(int value, DurationUnit unit)>> Parser =
        DurationRaw.AtLeastOnce();
}
