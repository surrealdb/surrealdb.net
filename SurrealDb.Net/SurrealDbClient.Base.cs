using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient : ISurrealDbClient
{
    internal ISurrealDbEngine Engine { get; set; } = null!;

    public Uri Uri { get; protected init; } = null!;

    protected void InitializeAndSetProviderEngine(
        ISurrealDbProviderEngine engine,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory,
        ISessionizer? sessionizer
    )
    {
        InitializeProviderEngine(
            engine,
            configuration,
            configureCborOptions,
            loggerFactory,
            sessionizer
        );
        Engine = engine;
    }

    internal static void InitializeProviderEngine(
        ISurrealDbProviderEngine engine,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory,
        ISessionizer? sessionizer
    )
    {
        engine.Initialize(
            configuration,
            configureCborOptions,
            loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null,
            sessionizer
        );
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

                {
                    var session = httpEngine.SessionInfos.Get(SessionId)!;

                    version = httpEngine._version;
                    ns = session.Ns;
                    db = session.Db;
                    auth = session.Auth;
                    configureCborOptions = httpEngine._configureCborOptions;
                }
                break;
            case SurrealDbWsEngine wsEngine:
                // 💡 Ensures underlying engine is started to retrieve some information
                await wsEngine.InternalConnectAsync(true, cancellationToken).ConfigureAwait(false);

                {
                    var session = wsEngine.SessionInfos.Get(SessionId)!;

                    version = wsEngine._version;
                    ns = session.Ns;
                    db = session.Db;
                    auth = session.Auth;
                    configureCborOptions = wsEngine._configureCborOptions;
                }
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
