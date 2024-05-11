using SurrealDb.Net.Internals.Constants;
#if NET5_0_OR_GREATER
using Pidgin;
using static Pidgin.Parser;
#else
using Superpower;
using Superpower.Parsers;
#endif

namespace SurrealDb.Net.Internals.Parsers;

internal static partial class DateTimeParser
{
    public static DateTime Convert(long seconds, int nanos)
    {
        return DateTime
            .UnixEpoch.AddSeconds(seconds)
            .AddTicks((long)Math.Round((double)nanos / TimeConstants.NanosecondsPerTick));
    }
}

#if NET5_0_OR_GREATER

internal static partial class DateTimeParser
{
    private static Parser<char, DateTime> Datetime =>
        DatetimeWithDelimiters.Or(DatetimeWithoutDelimiters);

    private static Parser<char, DateTime> DatetimeWithDelimiters =>
        from openingQuote in SingleOrDoubleQuote
        from datetime in DatetimeWithoutDelimiters
        from closingQuote in Char(openingQuote)
        select datetime;

    private static Parser<char, DateTime> DatetimeWithoutDelimiters =>
        DatetimeSingle.Or(DatetimeDouble);

    private static Parser<char, char> SingleOrDoubleQuote = Char('\'').Or(Char('\"'));

    private static Parser<char, DateTime> DatetimeSingle =>
        from datetime in DatetimeRaw
        select datetime;

    private static Parser<char, DateTime> DatetimeDouble =>
        from datetime in DatetimeRaw
        select datetime;

    private static Parser<char, DateTime> DatetimeRaw => Try(Nano).Or(Try(Time)).Or(Try(Date));

    private static Parser<char, DateTime> Date =>
        from year in Year
        from _ in Char('-')
        from month in Month
        from __ in Char('-')
        from day in Day
        select new DateTime(year, month, day);

    private static Parser<char, DateTime> Time =>
        from year in Year
        from _ in Char('-')
        from month in Month
        from __ in Char('-')
        from day in Day
        from ___ in Char('T')
        from hour in Hour
        from ____ in Char(':')
        from minute in Minute
        from _____ in Char(':')
        from second in Second
        from ______ in Zone
        select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static Parser<char, DateTime> Nano =>
        from year in Year
        from _ in Char('-')
        from month in Month
        from __ in Char('-')
        from day in Day
        from ___ in Char('T')
        from hour in Hour
        from ____ in Char(':')
        from minute in Minute
        from _____ in Char(':')
        from second in Second
        from ______ in Char('.')
        from nano in TakeUntilDigit
        from _______ in Zone
        let nanoVal = nano.PadRight(9, '0')[..9]
        let ticks = Math.Round(int.Parse(nanoVal) * TimeConstants.TicksPerNanosecond)
        select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddTicks(
            (long)ticks
        );

    private static Parser<char, int> Year =>
        from s in Sign
        from y in DecimalNum
        select s.GetValueOrDefault(1) * y;

    private static Parser<char, int> Month => from m in DecimalNum where m >= 1 && m <= 12 select m;

    private static Parser<char, int> Day => from d in DecimalNum where d >= 1 && d <= 31 select d;

    private static Parser<char, int> Hour => from h in DecimalNum where h >= 0 && h <= 23 select h;

    private static Parser<char, int> Minute =>
        from m in DecimalNum
        where m >= 0 && m <= 59
        select m;

    private static Parser<char, int> Second =>
        from s in DecimalNum
        where s >= 0 && s <= 60
        select s;

    private static Parser<char, TimeZoneInfo> Zone => ZoneUtc;

    private static Parser<char, TimeZoneInfo> ZoneUtc => Char('Z').Map(_ => TimeZoneInfo.Utc);

    private static Parser<char, Maybe<int>> Sign =>
        Char('-').Map(_ => -1).Or(Char('+').Map(_ => 1)).Optional();

    private static Parser<char, string> TakeUntilDigit =>
        Digit.AtLeastOnce().Select(chars => new string(chars.ToArray()));

    public static DateTime Parse(string input)
    {
        return Datetime.ParseOrThrow(input);
    }
}
#else
internal static partial class DateTimeParser
{
    private static TextParser<DateTime> Datetime =>
        DatetimeWithDelimiters.Or(DatetimeWithoutDelimiters);

    private static TextParser<DateTime> DatetimeWithDelimiters =>
        from openingQuote in SingleOrDoubleQuote
        from datetime in DatetimeWithoutDelimiters
        from closingQuote in Character.EqualTo(openingQuote)
        select datetime;

    private static TextParser<DateTime> DatetimeWithoutDelimiters =>
        DatetimeSingle.Or(DatetimeDouble);

    private static TextParser<char> SingleOrDoubleQuote = Character
        .EqualTo('\'')
        .Or(Character.EqualTo('\"'));

    private static TextParser<DateTime> DatetimeSingle =>
        from datetime in DatetimeRaw
        select datetime;

    private static TextParser<DateTime> DatetimeDouble =>
        from datetime in DatetimeRaw
        select datetime;

    private static TextParser<DateTime> DatetimeRaw => Nano.Try().Or(Time).Try().Or(Date);

    private static TextParser<DateTime> Date =>
        from year in Year
        from _ in Character.EqualTo('-')
        from month in Month
        from __ in Character.EqualTo('-')
        from day in Day
        select new DateTime(year, month, day);

    private static TextParser<DateTime> Time =>
        from year in Year
        from _ in Character.EqualTo('-')
        from month in Month
        from __ in Character.EqualTo('-')
        from day in Day
        from ___ in Character.EqualTo('T')
        from hour in Hour
        from ____ in Character.EqualTo(':')
        from minute in Minute
        from _____ in Character.EqualTo(':')
        from second in Second
        from ______ in Zone
        select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static TextParser<DateTime> Nano =>
        from year in Year
        from _ in Character.EqualTo('-')
        from month in Month
        from __ in Character.EqualTo('-')
        from day in Day
        from ___ in Character.EqualTo('T')
        from hour in Hour
        from ____ in Character.EqualTo(':')
        from minute in Minute
        from _____ in Character.EqualTo(':')
        from second in Second
        from ______ in Character.EqualTo('.')
        from nano in TakeUntilDigit
        from _______ in Zone
        let nanoVal = nano.PadRight(9, '0')[..9]
        let ticks = Math.Round(int.Parse(nanoVal) * TimeConstants.TicksPerNanosecond)
        select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddTicks(
            (long)ticks
        );

    private static TextParser<int> Year =>
        from s in Sign
        from y in Numerics.IntegerInt32
        select s * y;

    private static TextParser<int> Month =>
        from m in Numerics.IntegerInt32
        where m >= 1 && m <= 12
        select m;

    private static TextParser<int> Day =>
        from d in Numerics.IntegerInt32
        where d >= 1 && d <= 31
        select d;

    private static TextParser<int> Hour =>
        from h in Numerics.IntegerInt32
        where h >= 0 && h <= 23
        select h;

    private static TextParser<int> Minute =>
        from m in Numerics.IntegerInt32
        where m >= 0 && m <= 59
        select m;

    private static TextParser<int> Second =>
        from s in Numerics.IntegerInt32
        where s >= 0 && s <= 60
        select s;

    private static TextParser<TimeZoneInfo> Zone => ZoneUtc;

    private static TextParser<TimeZoneInfo> ZoneUtc => Span.EqualTo("Z").Value(TimeZoneInfo.Utc);

    private static TextParser<int> Sign =>
        Character.EqualTo('-').Value(-1).Or(Character.EqualTo('+').Value(1)).OptionalOrDefault(1);

    private static TextParser<string> TakeUntilDigit =>
        Character.Digit.AtLeastOnce().Select(chars => new string(chars));

    public static DateTime Parse(string input)
    {
        return Datetime.Parse(input);
    }
}
#endif
