using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Client;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Dependency injection extensions for the Agent Memory client.
/// </summary>
public static class AgentMemoryServiceCollectionExtensions
{
    private const string HttpClientNamePrefix = "SurrealDb.AgentMemory";

    /// <summary>
    /// Registers the Agent Memory client (as <see cref="IDefaultApi"/>) configured from
    /// <paramref name="configure"/>. Returns the <see cref="IHttpClientBuilder"/> so callers
    /// can add their own handlers or resilience policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the endpoint and API key.</param>
    public static IHttpClientBuilder AddAgentMemory(
        this IServiceCollection services,
        Action<AgentMemoryOptions> configure
    )
    {
        var options = BuildOptions(configure);

        services.AddLogging();

        services.AddSingleton<TokenProvider<BearerToken>>(_ => new StaticBearerTokenProvider(
            options.ApiKey
        ));
        services.AddSingleton(
            new JsonSerializerOptionsProvider(
                options.JsonSerializerOptions ?? AgentMemoryJson.DefaultOptions
            )
        );
        services.AddSingleton<DefaultApiEvents>();
        services.AddTransient<IDefaultApi>(sp => sp.GetRequiredService<DefaultApi>());

        return services.AddHttpClient<DefaultApi>(client => ConfigureClient(client, options));
    }

    /// <summary>
    /// Registers a keyed Agent Memory client (resolvable via
    /// <c>[FromKeyedServices(name)] IDefaultApi</c> or
    /// <c>provider.GetRequiredKeyedService&lt;IDefaultApi&gt;(name)</c>), useful when an
    /// application talks to several Agent Memory deployments or contexts.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key under which the client is registered.</param>
    /// <param name="configure">Configures the endpoint and API key.</param>
    public static IHttpClientBuilder AddKeyedAgentMemory(
        this IServiceCollection services,
        string name,
        Action<AgentMemoryOptions> configure
    )
    {
        var options = BuildOptions(configure);
        var httpClientName = $"{HttpClientNamePrefix}:{name}";

        services.AddLogging();

        services.AddKeyedSingleton<TokenProvider<BearerToken>>(
            name,
            (_, _) => new StaticBearerTokenProvider(options.ApiKey)
        );
        services.AddKeyedSingleton<JsonSerializerOptionsProvider>(
            name,
            (_, _) =>
                new JsonSerializerOptionsProvider(
                    options.JsonSerializerOptions ?? AgentMemoryJson.DefaultOptions
                )
        );
        services.AddKeyedSingleton<DefaultApiEvents>(name);
        services.AddKeyedTransient<DefaultApi>(name, CreateApi(httpClientName));
        services.AddKeyedTransient<IDefaultApi>(
            name,
            (sp, key) => sp.GetRequiredKeyedService<DefaultApi>(key)
        );

        return services.AddHttpClient(httpClientName, client => ConfigureClient(client, options));
    }

    private static Func<IServiceProvider, object?, DefaultApi> CreateApi(string httpClientName) =>
        (sp, key) =>
            new DefaultApi(
                sp.GetRequiredService<ILogger<DefaultApi>>(),
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName),
                sp.GetRequiredKeyedService<JsonSerializerOptionsProvider>(key),
                sp.GetRequiredKeyedService<DefaultApiEvents>(key),
                sp.GetRequiredKeyedService<TokenProvider<BearerToken>>(key)
            );

    private static AgentMemoryOptions BuildOptions(Action<AgentMemoryOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new AgentMemoryOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new ArgumentException(
                "AgentMemoryOptions.Endpoint must be set (e.g. https://mem.example.com).",
                nameof(configure)
            );
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                "AgentMemoryOptions.ApiKey must be set.",
                nameof(configure)
            );
        }

        return options;
    }

    private static void ConfigureClient(HttpClient client, AgentMemoryOptions options)
    {
        try
        {
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
        }
        catch (UriFormatException ex)
        {
            throw new ArgumentException(
                $"AgentMemoryOptions.Endpoint '{options.Endpoint}' is not a valid absolute URI.",
                nameof(options),
                ex
            );
        }

        if (options.RequestTimeout is { } timeout)
        {
            client.Timeout = timeout;
        }
    }

    private sealed class StaticBearerTokenProvider(string token) : TokenProvider<BearerToken>
    {
        protected internal override ValueTask<BearerToken> GetAsync(
            string header = "",
            System.Threading.CancellationToken cancellation = default
        ) => new(new BearerToken(token));
    }
}
