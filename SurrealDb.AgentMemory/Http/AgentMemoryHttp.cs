namespace SurrealDb.AgentMemory;

/// <summary>
/// The HTTP plumbing for clients this package creates itself.
/// </summary>
internal static class AgentMemoryHttp
{
    private static readonly Lazy<HttpClient> SharedClient = new(
        () =>
            new HttpClient(CreateHandler())
            {
                // Deadlines are applied per request, so uploads are not cut off by a client-wide timeout.
                Timeout = Timeout.InfiniteTimeSpan,
            }
    );

    /// <summary>
    /// The client behind every <see cref="AgentMemoryClient"/> created without an <see cref="HttpClient"/>,
    /// shared the way .NET recommends for long-lived clients.
    /// </summary>
    public static HttpClient Shared => SharedClient.Value;

    /// <summary>
    /// A new handler with the settings a client needs when it carries an API key.
    /// </summary>
    public static HttpMessageHandler CreateHandler()
    {
        if (!SocketsHttpHandler.IsSupported)
        {
            // In the browser (Blazor WebAssembly) requests go through fetch, which has none of these settings.
            return new HttpClientHandler();
        }

        return new SocketsHttpHandler
        {
            // Connections are recycled, so a client that lives as long as the process picks up DNS changes.
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            // The handler can serve clients that hold different API keys: a cookie set in answer to one
            // of them must not ride along with the requests of another.
            UseCookies = false,
            // A redirect drops the Authorization header, so following one turns every call into a 401
            // that blames the key. The redirect is reported instead.
            AllowAutoRedirect = false,
        };
    }
}
