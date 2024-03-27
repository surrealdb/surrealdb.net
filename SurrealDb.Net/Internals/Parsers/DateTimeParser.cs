#if NET5_0_OR_GREATER
using Pidgin;
using SurrealDb.Net.Internals.Constants;
using static Pidgin.Parser;

namespace SurrealDb.Net.Internals.Parsers;

internal static class DateTimeParser
{
    public static Parser<char, DateTime> Datetime =>
        DatetimeWithDelimiters.Or(DatetimeWithoutDelimiters);

    public static Parser<char, DateTime> DatetimeWithDelimiters =>
        from openingQuote in SingleOrDoubleQuote
        from datetime in DatetimeWithoutDelimiters
        from closingQuote in Char(openingQuote)
        select datetime;

    public static Parser<char, DateTime> DatetimeWithoutDelimiters =>
        DatetimeSingle.Or(DatetimeDouble);

    public static Parser<char, char> SingleOrDoubleQuote = Char('\'').Or(Char('\"'));

    public static Parser<char, DateTime> DatetimeSingle =>
        from datetime in DatetimeRaw
        select datetime;

    public static Parser<char, DateTime> DatetimeDouble =>
        from datetime in DatetimeRaw
        select datetime;

    public static Parser<char, DateTime> DatetimeRaw => Try(Nano).Or(Try(Time)).Or(Try(Date));

    public static Parser<char, DateTime> Date =>
        from year in Year
        from _ in Char('-')
        from month in Month
        from __ in Char('-')
        from day in Day
        select new DateTime(year, month, day);

    public static Parser<char, DateTime> Time =>
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

    public static Parser<char, DateTime> Nano =>
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

    public static Parser<char, int> Year =>
        from s in Sign
        from y in DecimalNum
        select s.GetValueOrDefault(1) * y;

    public static Parser<char, int> Month => from m in DecimalNum where m >= 1 && m <= 12 select m;

    public static Parser<char, int> Day => from d in DecimalNum where d >= 1 && d <= 31 select d;

    public static Parser<char, int> Hour => from h in DecimalNum where h >= 0 && h <= 23 select h;

    public static Parser<char, int> Minute => from m in DecimalNum where m >= 0 && m <= 59 select m;

    public static Parser<char, int> Second => from s in DecimalNum where s >= 0 && s <= 60 select s;

    public static Parser<char, TimeZoneInfo> Zone => ZoneUtc;

    public static Parser<char, TimeZoneInfo> ZoneUtc => Char('Z').Map(_ => TimeZoneInfo.Utc);

    public static Parser<char, Maybe<int>> Sign =>
        Char('-').Map(_ => -1).Or(Char('+').Map(_ => 1)).Optional();

    public static Parser<char, string> TakeUntilDigit =>
        Digit.AtLeastOnce().Select(chars => new string(chars.ToArray()));

    public static DateTime Parse(string input)
    {
        return Datetime.ParseOrThrow(input);
    }
}
#else
using Superpower;
using Superpower.Parsers;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Internals.Parsers;

internal static class DateTimeParser
{
    public static TextParser<DateTime> Datetime =>
        DatetimeWithDelimiters.Or(DatetimeWithoutDelimiters);

    public static TextParser<DateTime> DatetimeWithDelimiters =>
        from openingQuote in SingleOrDoubleQuote
        from datetime in DatetimeWithoutDelimiters
        from closingQuote in Character.EqualTo(openingQuote)
        select datetime;

    public static TextParser<DateTime> DatetimeWithoutDelimiters =>
        DatetimeSingle.Or(DatetimeDouble);

    public static TextParser<char> SingleOrDoubleQuote = Character
        .EqualTo('\'')
        .Or(Character.EqualTo('\"'));

    public static TextParser<DateTime> DatetimeSingle =>
        from datetime in DatetimeRaw
        select datetime;

    public static TextParser<DateTime> DatetimeDouble =>
        from datetime in DatetimeRaw
        select datetime;

    public static TextParser<DateTime> DatetimeRaw => Nano.Try().Or(Time).Try().Or(Date);

    public static TextParser<DateTime> Date =>
        from year in Year
        from _ in Character.EqualTo('-')
        from month in Month
        from __ in Character.EqualTo('-')
        from day in Day
        select new DateTime(year, month, day);

    public static TextParser<DateTime> Time =>
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

    public static TextParser<DateTime> Nano =>
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

    public static TextParser<int> Year =>
        from s in Sign
        from y in Numerics.IntegerInt32
        select s * y;

    public static TextParser<int> Month =>
        from m in Numerics.IntegerInt32
        where m >= 1 && m <= 12
        select m;

    public static TextParser<int> Day =>
        from d in Numerics.IntegerInt32
        where d >= 1 && d <= 31
        select d;

    public static TextParser<int> Hour =>
        from h in Numerics.IntegerInt32
        where h >= 0 && h <= 23
        select h;

    public static TextParser<int> Minute =>
        from m in Numerics.IntegerInt32
        where m >= 0 && m <= 59
        select m;

    public static TextParser<int> Second =>
        from s in Numerics.IntegerInt32
        where s >= 0 && s <= 60
        select s;

    public static TextParser<TimeZoneInfo> Zone => ZoneUtc;

    public static TextParser<TimeZoneInfo> ZoneUtc => Span.EqualTo("Z").Value(TimeZoneInfo.Utc);

    public static TextParser<int> Sign =>
        Character.EqualTo('-').Value(-1).Or(Character.EqualTo('+').Value(1)).OptionalOrDefault(1);

    public static TextParser<string> TakeUntilDigit =>
        Character.Digit.AtLeastOnce().Select(chars => new string(chars));

    public static DateTime Parse(string input)
    {
        return Datetime.Parse(input);
    }
}
#endif
