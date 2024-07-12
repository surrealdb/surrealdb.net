#if NET6_0_OR_GREATER
namespace SurrealDb.Net.Internals.Formatters;

internal static class DateOnlyFormatter
{
    public static long Convert(DateOnly value)
    {
        var diff =
            value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToUniversalTime()
            - DateTime.UnixEpoch;

        return (long)diff.TotalSeconds;
    }
}
#endif
