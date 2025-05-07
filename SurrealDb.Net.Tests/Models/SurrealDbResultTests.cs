using System.Net;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests.Models;

public class SurrealDbResultTests
{
    public static class TestDataSources
    {
        public static IEnumerable<Func<(ISurrealDbResult, bool)>> IsOkResultCases()
        {
            yield return () =>
                (
                    new SurrealDbOkResult(
                        TimeSpan.Zero,
                        "OK",
                        SurrealDbResponseType.Other,
                        new ReadOnlyMemory<byte>(),
                        null!
                    ),
                    true
                );
            yield return () =>
                (new SurrealDbErrorResult(TimeSpan.Zero, "KO", "Something went wrong..."), false);
            yield return () =>
                (
                    new SurrealDbProtocolErrorResult(
                        HttpStatusCode.UnprocessableContent,
                        "",
                        "",
                        ""
                    ),
                    false
                );
            yield return () => (new SurrealDbUnknownResult(), false);
        }
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.IsOkResultCases))]
    public void ShouldTestIsOk(ISurrealDbResult value, bool expected)
    {
        value.IsOk.Should().Be(expected);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.IsOkResultCases))]
    public void ShouldTestIsError(ISurrealDbResult value, bool expected)
    {
        value.IsError.Should().Be(!expected);
    }
}
