using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests.Fixtures;

/// <summary>
/// Skips a test unless the server negotiates <c>cbor.2</c>, which carries SurrealDB 4.0's sized
/// numbers.
/// </summary>
public sealed class RequiresCbor2Attribute()
    : SkipAttribute("This test needs a server that negotiates cbor.2 (SurrealDB 4.0 or later).")
{
    public override async Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        int index = context
            .TestDetails.MethodMetadata.Parameters.Index()
            .First(x => x.Item.Name == "connectionString")
            .Index;

        var connectionString = (string)context.TestDetails.TestMethodArguments[index]!;

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        return !await IsNegotiatedAsync(client);
    }

    /// <summary>
    /// Whether <paramref name="client"/> talks <c>cbor.2</c> to its server. Over HTTP the client
    /// offers cbor.2 once it knows the server version, from its first request, and learns the
    /// answer from the Content-Type of the next response, so this sends two requests.
    /// </summary>
    internal static async Task<bool> IsNegotiatedAsync(SurrealDbClient client)
    {
        await client.Version();
        await client.Version();

        return client.Engine switch
        {
            SurrealDbWsEngine wsEngine => wsEngine.IsCbor2,
            SurrealDbHttpEngine httpEngine => httpEngine.IsCbor2,
            _ => false,
        };
    }
}
