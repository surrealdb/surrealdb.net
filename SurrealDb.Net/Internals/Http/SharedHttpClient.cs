namespace SurrealDb.Net.Internals.Http;

/// <summary>
/// A single <see cref="HttpClient"/> shared by every client that has no <see cref="IHttpClientFactory"/>,
/// so that calls to the HTTP endpoints (import, export) reuse pooled connections
/// instead of opening a new socket each time.
/// </summary>
internal static class SharedHttpClient
{
    private static readonly Lazy<HttpClient> _instance = new(Create, true);

    public static HttpClient Instance => _instance.Value;

    internal static HttpClient Create()
    {
        // 💡 Cookies are never stored: this client is shared by every SDK client of the process,
        // which must not see each other's cookies
#if NET5_0_OR_GREATER
        // 💡 Recycle pooled connections so that DNS changes are picked up
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            UseCookies = false,
        };
        return new HttpClient(handler);
#else
        return new HttpClient(new HttpClientHandler { UseCookies = false });
#endif
    }
}
