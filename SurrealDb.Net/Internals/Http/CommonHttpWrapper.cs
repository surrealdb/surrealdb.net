using Dahomey.Cbor;
using Semver;

namespace SurrealDb.Net.Internals.Http;

internal sealed record CommonHttpWrapper(
    HttpClient HttpClient,
    SemVersion? Version,
    Action<CborOptions>? ConfigureCborOptions
) : IDisposable
{
    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
