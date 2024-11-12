using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Tests.Extensions;

public class SurrealDbLoggerExtensionsTests
{
    [Theory]
    [InlineData(null, false, "?")]
    [InlineData(null, true, "null")]
    [InlineData(true, false, "?")]
    [InlineData(true, true, "'True'")]
    [InlineData(34, false, "?")]
    [InlineData(34, true, "'34'")]
    [InlineData("Hello world", false, "?")]
    [InlineData("Hello world", true, "'Hello world'")]
    public void ShouldFormatParameterValue(
        object? value,
        bool shouldLogParameterValue,
        string expected
    )
    {
        SurrealDbLoggerExtensions
            .FormatParameterValue(value, shouldLogParameterValue)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void ShouldFormatRequestParametersWithSensitive()
    {
        string result = SurrealDbLoggerExtensions.FormatRequestParameters(
            [1, "Hello", "test"],
            false
        );
        result.Should().Be("[?, ?, ?]");
    }

    [Fact]
    public void ShouldFormatRequestParametersWithoutSensitive()
    {
        string result = SurrealDbLoggerExtensions.FormatRequestParameters(
            [1, "Hello", "test"],
            true
        );
        result.Should().Be("['1', 'Hello', 'test']");
    }

    [Fact]
    public void ShouldFormatQueryParametersWithSensitive()
    {
        string result = SurrealDbLoggerExtensions.FormatQueryParameters(
            new Dictionary<string, object?>()
            {
                { "p0", 1 },
                { "p1", "Hello" },
                { "p2", "test" },
            },
            false
        );
        result.Should().Be("$p0=?, $p1=?, $p2=?");
    }

    [Fact]
    public void ShouldFormatQueryParametersWithoutSensitive()
    {
        string result = SurrealDbLoggerExtensions.FormatQueryParameters(
            new Dictionary<string, object?>()
            {
                { "p0", 1 },
                { "p1", "Hello" },
                { "p2", "test" },
            },
            true
        );
        result.Should().Be("$p0='1', $p1='Hello', $p2='test'");
    }

    [Fact]
    public void ShouldFormatExecutionTimeAsSecond()
    {
        string result = SurrealDbLoggerExtensions.FormatExecutionTime(TimeSpan.FromSeconds(3));
        result.Should().Be("3s");
    }

    [Fact]
    public void ShouldFormatExecutionTimeAsMillisecond()
    {
        string result = SurrealDbLoggerExtensions.FormatExecutionTime(
            TimeSpan.FromMilliseconds(14)
        );
        result.Should().Be("14ms");
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void ShouldFormatExecutionTimeAsMicrosecond()
    {
        string result = SurrealDbLoggerExtensions.FormatExecutionTime(
            TimeSpan.FromMicroseconds(42)
        );
        result.Should().Be("42us");
    }
#endif
}
