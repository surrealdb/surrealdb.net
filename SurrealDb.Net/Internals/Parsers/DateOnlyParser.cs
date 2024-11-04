#if NET6_0_OR_GREATER
namespace SurrealDb.Net.Internals.Parsers;

internal static class DateOnlyParser
{
    public static DateOnly Convert(long seconds, int nanos)
    {
        var dateTime = DateTimeParser.Convert(seconds, nanos);
        return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
    }
}
#endif
