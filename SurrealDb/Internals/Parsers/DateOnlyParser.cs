#if NET6_0_OR_GREATER
namespace SurrealDb.Internals.Parsers;

internal static class DateOnlyParser
{
	public static DateOnly Parse(string input)
	{
		var dateTime = DateTimeParser.Parse(input);
		return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
	}
}
#endif
