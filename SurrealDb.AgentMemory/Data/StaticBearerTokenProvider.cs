using SurrealDb.AgentMemory.Client;

namespace SurrealDb.AgentMemory;

internal sealed class StaticBearerTokenProvider(string token) : TokenProvider<BearerToken>
{
    protected internal override ValueTask<BearerToken> GetAsync(
        string header = "",
        CancellationToken cancellation = default
    ) => new(new BearerToken(token));
}
