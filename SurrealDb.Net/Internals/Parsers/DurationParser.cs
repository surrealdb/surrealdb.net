using Superpower.Parsers;
using Superpower;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals.Parsers;

internal static class DurationParser
{
    public static readonly TextParser<DurationUnit> DurationUnitParser = Span.EqualTo("ns")
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

    public static readonly TextParser<(decimal value, DurationUnit unit)> DurationRaw =
        from v in Numerics.DecimalDecimal
        from u in DurationUnitParser!
        select (v, u);

    public static readonly TextParser<(decimal value, DurationUnit unit)[]> Parser =
        DurationRaw.AtLeastOnce();
}
