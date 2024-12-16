using System.Net;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests.Models;

public class SurrealDbResultTests
{
    public static TheoryData<ISurrealDbResult, bool> IsOkResultCases =>
        new()
        {
            { new SurrealDbOkResult(TimeSpan.Zero, "OK", new ReadOnlyMemory<byte>(), null!), true },
            { new SurrealDbErrorResult(TimeSpan.Zero, "KO", "Something went wrong..."), false },
#if NET8_0_OR_GREATER
            {
                new SurrealDbProtocolErrorResult(HttpStatusCode.UnprocessableContent, "", "", ""),
                false
            },
#else
            {
                new SurrealDbProtocolErrorResult(HttpStatusCode.UnprocessableEntity, "", "", ""),
                false
            },
#endif
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
