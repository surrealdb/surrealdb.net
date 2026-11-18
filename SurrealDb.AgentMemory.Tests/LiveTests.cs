using FluentAssertions;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/integration/live.test.ts</c>: runs against a real deployment when
/// <c>AGENT_MEMORY_ENDPOINT</c> and <c>AGENT_MEMORY_API_KEY</c> are set, and is skipped otherwise.
/// </summary>
public class LiveTests
{
    private static readonly string Endpoint =
        Environment.GetEnvironmentVariable("AGENT_MEMORY_ENDPOINT") ?? "";

    private static readonly string ApiKey =
        Environment.GetEnvironmentVariable("AGENT_MEMORY_API_KEY") ?? "";

    [Test]
    public async Task HealthCheck()
    {
        Skip.When(
            Endpoint.Length == 0 || ApiKey.Length == 0,
            "AGENT_MEMORY_ENDPOINT and AGENT_MEMORY_API_KEY must be set for live tests."
        );

        var client = new AgentMemoryClient(
            new AgentMemoryOptions
            {
                Context = "default",
                ApiKey = ApiKey,
                Endpoint = Endpoint,
            }
        );

        await client.HealthAsync();

        client.ContextId.Should().Be("default");
    }
}
