using SurrealDb.Net.Models.Response;
using System.Text.Json;

namespace SurrealDb.Tests.Models;

public class SurrealDbResultTests
{
    public static TheoryData<ISurrealDbResult, bool> IsOkResultCases =>
        new()
        {
            { new SurrealDbOkResult(TimeSpan.FromSeconds(0), "OK", new JsonElement()), true },
            { new SurrealDbErrorResult(), false },
            { new SurrealDbProtocolErrorResult(), false },
            { new SurrealDbUnknownResult(), false },
        };

    [Theory]
    [MemberData(nameof(IsOkResultCases))]
    public void ShouldTestIsOk(ISurrealDbResult value, bool expected)
    {
        value.IsOk.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(IsOkResultCases))]
    public void ShouldTestIsError(ISurrealDbResult value, bool expected)
    {
        value.IsError.Should().Be(!expected);
    }
}
