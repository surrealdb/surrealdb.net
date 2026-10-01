using Microsoft.Extensions.Logging;
using SurrealDb.AgentMemory;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Client;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection extensions for the Agent Memory client.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string HttpClientNamePrefix = "SurrealDb.AgentMemory";

    /// <summary>
    /// Registers the Agent Memory client (as <see cref="IAgentMemoryApi"/>) configured from <paramref name="configure"/>.
    /// Returns the <see cref="IHttpClientBuilder"/> so callers can add their own handlers or resilience policies.
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
                options.JsonSerializerOptions ?? AgentMemoryJsonExtensions.DefaultOptions
            )
        );
        services.AddSingleton<AgentMemoryApiEvents>();
        services.AddTransient<IAgentMemoryApi>(sp => sp.GetRequiredService<AgentMemoryApi>());

        return services.AddHttpClient<AgentMemoryApi>(client => ConfigureClient(client, options));
    }

    /// <summary>
    /// Registers a keyed Agent Memory client: resolvable via <c>[FromKeyedServices(name)] IAgentMemoryApi</c> or <c>provider.GetRequiredKeyedService&lt;IAgentMemoryApi&gt;(name)</c>.
    /// Useful when an application talks to several Agent Memory deployments or contexts.
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
                    options.JsonSerializerOptions ?? AgentMemoryJsonExtensions.DefaultOptions
                )
        );
        services.AddKeyedSingleton<AgentMemoryApiEvents>(name);
        services.AddKeyedTransient<AgentMemoryApi>(name, CreateApi(httpClientName));
        services.AddKeyedTransient<IAgentMemoryApi>(
            name,
            (sp, key) => sp.GetRequiredKeyedService<AgentMemoryApi>(key)
        );

        return services.AddHttpClient(httpClientName, client => ConfigureClient(client, options));
    }

    private static Func<IServiceProvider, object?, AgentMemoryApi> CreateApi(
        string httpClientName
    ) =>
        (sp, key) =>
            new AgentMemoryApi(
                sp.GetRequiredService<ILogger<AgentMemoryApi>>(),
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName),
                sp.GetRequiredKeyedService<JsonSerializerOptionsProvider>(key),
                sp.GetRequiredKeyedService<AgentMemoryApiEvents>(key),
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
}
