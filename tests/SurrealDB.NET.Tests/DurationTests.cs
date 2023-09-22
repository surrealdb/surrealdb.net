using System.Text;
using SurrealDB.NET.Json.Converters;

namespace SurrealDB.NET.Tests;

[Trait("Category", "Parsing")]
public sealed class DurationTests
{
    [Theory(DisplayName = "Valid duration")]
    [InlineData("1y2w3d4h5m6s7ms8\u00b5s", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7 + 3 * TimeSpan.TicksPerDay + 4 * TimeSpan.TicksPerHour + 5 * TimeSpan.TicksPerMinute + 6 * TimeSpan.TicksPerSecond + 7 * TimeSpan.TicksPerMillisecond + 8 * TimeSpan.TicksPerMicrosecond)]
    [InlineData("1y2w3d4h5m6s7ms", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7 + 3 * TimeSpan.TicksPerDay + 4 * TimeSpan.TicksPerHour + 5 * TimeSpan.TicksPerMinute + 6 * TimeSpan.TicksPerSecond + 7 * TimeSpan.TicksPerMillisecond)]
    [InlineData("1y2w3d4h5m6s", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7 + 3 * TimeSpan.TicksPerDay + 4 * TimeSpan.TicksPerHour + 5 * TimeSpan.TicksPerMinute + 6 * TimeSpan.TicksPerSecond)]
    [InlineData("1y2w3d4h5m", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7 + 3 * TimeSpan.TicksPerDay + 4 * TimeSpan.TicksPerHour + 5 * TimeSpan.TicksPerMinute)]
    [InlineData("1.0y2.0w3.0d4.0h5.0m6.0s7.0ms8.0\u00b5s", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7 + 3 * TimeSpan.TicksPerDay + 4 * TimeSpan.TicksPerHour + 5 * TimeSpan.TicksPerMinute + 6 * TimeSpan.TicksPerSecond + 7 * TimeSpan.TicksPerMillisecond + 8 * TimeSpan.TicksPerMicrosecond)]
    [InlineData("1y", TimeSpan.TicksPerDay * 365)]
    [InlineData("1.0y", TimeSpan.TicksPerDay * 365)]
    [InlineData("1.0y2.0w", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7)]
    [InlineData("1.0y2.0w3.0d", TimeSpan.TicksPerDay * 365 + 2 * TimeSpan.TicksPerDay * 7 + 3 * TimeSpan.TicksPerDay)]
    [InlineData("1w", TimeSpan.TicksPerDay * 7)]
    [InlineData("1.0w", TimeSpan.TicksPerDay * 7)]
    [InlineData("1d", TimeSpan.TicksPerDay)]
    [InlineData("1.0d", TimeSpan.TicksPerDay)]
    [InlineData("1h", TimeSpan.TicksPerHour)]
    [InlineData("1.0h", TimeSpan.TicksPerHour)]
    [InlineData("1m", TimeSpan.TicksPerMinute)]
    [InlineData("1.0m", TimeSpan.TicksPerMinute)]
    [InlineData("1s", TimeSpan.TicksPerSecond)]
    [InlineData("1.0s", TimeSpan.TicksPerSecond)]
    [InlineData("1ms", TimeSpan.TicksPerMillisecond)]
    [InlineData("1.0ms", TimeSpan.TicksPerMillisecond)]
    [InlineData("1\u00b5s", TimeSpan.TicksPerMicrosecond)]
    [InlineData("1.0\u00b5s", TimeSpan.TicksPerMicrosecond)]
    public void ValidFormats(string input, long ticks)
    {
        var ts = SurrealTimeSpanJsonConverter.ParseDuration(Encoding.UTF8.GetBytes(input));
        Assert.Equal(ticks, ts.Ticks);
    }
}
