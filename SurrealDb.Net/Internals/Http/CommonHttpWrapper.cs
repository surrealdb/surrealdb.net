using Dahomey.Cbor;
using Semver;
using SurrealDb.Net.Internals.Auth;

namespace SurrealDb.Net.Internals.Http;

internal sealed record CommonHttpWrapper(
    HttpClientWrapper HttpClientWrapper,
    SemVersion? Version,
    string Ns,
    string Db,
    IAuth? Auth,
    Action<CborOptions>? ConfigureCborOptions
) : IDisposable
{
    public HttpClient HttpClient => HttpClientWrapper.Instance;

    /// <summary>
    /// Creates a request carrying the namespace, database and auth headers of the current session.
    /// </summary>
    public HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, uri) { Content = content };

        SurrealDbHttpEngine.SetNsDbHttpClientHeaders(request.Headers, Version, Ns, Db);
        SurrealDbHttpEngine.SetAuthHttpClientHeaders(request.Headers, Auth);

        return request;
    }

    public void Dispose()
    {
        HttpClientWrapper.Dispose();
    }
}
