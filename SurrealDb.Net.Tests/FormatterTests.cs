using SurrealDb.Net.Internals.Formatters;

namespace SurrealDb.Net.Tests;

public class FormatterTests
{
    public static TheoryData<TimeSpan, string> TimeSpanFormatterCases =>
        new()
        {
            { TimeSpan.FromSeconds(0), "0ns" },
            { TimeSpan.FromMilliseconds(0.000_001), "0ns" }, // Cannot go under 100ns with TimeSpan
            { TimeSpan.FromMilliseconds(0.000_01), "0ns" }, // Cannot go under 100ns with TimeSpan
            { TimeSpan.FromMilliseconds(0.000_1), "100ns" },
            { TimeSpan.FromMilliseconds(0.001), "1µs" },
            { TimeSpan.FromMilliseconds(1), "1ms" },
            { TimeSpan.FromSeconds(1), "1s" },
            { TimeSpan.FromSeconds(30.25), "30s250ms" },
            { TimeSpan.FromMinutes(1), "1m" },
            { TimeSpan.FromHours(1), "1h" },
            { TimeSpan.FromDays(1), "1d" },
            { TimeSpan.FromDays(7), "1w" },
            { TimeSpan.FromDays(365), "1y" },
            { TimeSpan.FromSeconds(458215784), "14y27w4d10h9m44s" },
        };

    [Theory]
    [MemberData(nameof(TimeSpanFormatterCases))]
    public void ShouldFormatTimeSpan(TimeSpan value, string expected)
    {
        string result = TimeSpanFormatter.Format(value);
        result.Should().Be(expected);
    }
}
