#if NET6_0_OR_GREATER
namespace SurrealDb.Internals.Parsers;

internal class TimeOnlyParser
{
	public static TimeOnly Parse(string input)
	{
		var timeSpan = TimeSpanParser.Parse(input);
		return new TimeOnly(timeSpan.Ticks % TimeSpan.TicksPerDay);
	}
}
#endif
