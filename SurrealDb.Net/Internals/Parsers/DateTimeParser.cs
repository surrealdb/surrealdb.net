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
