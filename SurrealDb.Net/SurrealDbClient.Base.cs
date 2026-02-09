using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient : ISurrealDbClient
{
    protected Func<Task>? _poolTask { get; set; }
    internal ISurrealDbEngine Engine { get; set; } = null!;

    public Uri Uri { get; protected set; } = null!;

    protected void InitializeProviderEngine(
        ISurrealDbProviderEngine engine,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory
    )
    {
        engine.Initialize(
            configuration,
            configureCborOptions,
            loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null
        );
        Engine = engine;
    }

    private async Task<CommonHttpWrapper> CreateCommonHttpWrapperAsync(
        CancellationToken cancellationToken
    )
    {
        SemVersion? version;
        string? ns;
        string? db;
        IAuth? auth;
        Action<CborOptions>? configureCborOptions;

        switch (Engine)
        {
            case SurrealDbHttpEngine httpEngine:
                // 💡 Ensures underlying engine is started to retrieve some information
                await httpEngine.Connect(cancellationToken).ConfigureAwait(false);

                version = httpEngine._version;
                ns = httpEngine._config.Ns;
                db = httpEngine._config.Db;
                auth = httpEngine._config.Auth;
                configureCborOptions = httpEngine._configureCborOptions;
                break;
            case SurrealDbWsEngine wsEngine:
                // 💡 Ensures underlying engine is started to retrieve some information
                await wsEngine.InternalConnectAsync(true, cancellationToken).ConfigureAwait(false);

                version = wsEngine._version;
                ns = wsEngine._config.Ns;
                db = wsEngine._config.Db;
                auth = wsEngine._config.Auth;
                configureCborOptions = wsEngine._configureCborOptions;
                break;
            default:
                throw new SurrealDbException("No underlying engine is started.");
        }

        if (string.IsNullOrWhiteSpace(ns))
        {
            throw new SurrealDbException("Namespace should be provided to export data.");
        }
        if (string.IsNullOrWhiteSpace(db))
        {
            throw new SurrealDbException("Database should be provided to export data.");
        }

        var httpClient = new HttpClient();

        SurrealDbHttpEngine.SetNsDbHttpClientHeaders(httpClient, version, ns, db);
        SurrealDbHttpEngine.SetAuthHttpClientHeaders(httpClient, auth);

        return new CommonHttpWrapper(httpClient, version, configureCborOptions);
    }
}
