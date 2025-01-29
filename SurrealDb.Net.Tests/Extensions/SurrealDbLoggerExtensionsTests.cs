using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Tests.Extensions;

public class SurrealDbLoggerExtensionsTests
{
    [Test]
    [Arguments(null, false, "?")]
    [Arguments(null, true, "null")]
    [Arguments(true, false, "?")]
    [Arguments(true, true, "'True'")]
    [Arguments(34, false, "?")]
    [Arguments(34, true, "'34'")]
    [Arguments("Hello world", false, "?")]
    [Arguments("Hello world", true, "'Hello world'")]
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

    [Test]
    public void ShouldFormatRequestParametersWithSensitive()
    {
        string result = SurrealDbLoggerExtensions.FormatRequestParameters(
            [1, "Hello", "test"],
            false
        );
        result.Should().Be("[?, ?, ?]");
    }

    [Test]
    public void ShouldFormatRequestParametersWithoutSensitive()
    {
        string result = SurrealDbLoggerExtensions.FormatRequestParameters(
            [1, "Hello", "test"],
            true
        );
        result.Should().Be("['1', 'Hello', 'test']");
    }

    [Test]
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

    [Test]
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

    [Test]
    public void ShouldFormatExecutionTimeAsSecond()
    {
        string result = SurrealDbLoggerExtensions.FormatExecutionTime(TimeSpan.FromSeconds(3));
        result.Should().Be("3s");
    }

    [Test]
    public void ShouldFormatExecutionTimeAsMillisecond()
    {
        string result = SurrealDbLoggerExtensions.FormatExecutionTime(
            TimeSpan.FromMilliseconds(14)
        );
        result.Should().Be("14ms");
    }

#if NET7_0_OR_GREATER
    [Test]
    public void ShouldFormatExecutionTimeAsMicrosecond()
    {
        string result = SurrealDbLoggerExtensions.FormatExecutionTime(
            TimeSpan.FromMicroseconds(42)
        );
        result.Should().Be("42us");
    }
#endif
}
