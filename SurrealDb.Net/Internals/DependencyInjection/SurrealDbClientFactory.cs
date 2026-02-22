using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SurrealDb.Net.Internals.DependencyInjection;

internal sealed class SurrealDbClientFactory
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private SurrealDbClient? _rootClient;

    public ISurrealDbSession CreateChildClient(
        IServiceProvider serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ISessionizer? sessionizer
    )
    {
        lock (_lock)
        {
            _rootClient ??= CreateSurrealDbClient(
                serviceProvider,
                configuration,
                configureCborOptions,
                sessionizer
            );
        }

        var sessionId = Guid.NewGuid();
        return new SurrealDbSession(_rootClient, sessionId);
    }

    public static SurrealDbClient CreateSurrealDbClient(
        IServiceProvider serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ISessionizer? sessionizer
    )
    {
        return new SurrealDbClient(
            configuration,
            serviceProvider,
            serviceProvider.GetService<IHttpClientFactory>(),
            configureCborOptions,
            serviceProvider.GetService<ILoggerFactory>(),
            sessionizer
        );
    }
}
