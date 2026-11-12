using SurrealDb.AgentMemory;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection for <see cref="AgentMemoryClient"/>.
/// </summary>
public static class AgentMemoryServiceCollectionExtensions
{
    private const string HttpClientNamePrefix = "SurrealDb.AgentMemory";

    /// <summary>
    /// Registers an <see cref="AgentMemoryClient"/> configured by <paramref name="configure"/>.
    /// </summary>
    /// <remarks>
    /// The options are read and validated once, here. The client is transient and holds nothing to dispose, so the
    /// container does not keep the instances it hands out, even ones resolved from the root provider. Its
    /// <see cref="HttpClient"/> comes from <see cref="IHttpClientFactory"/>, and the returned builder adds handlers to
    /// it. The client retries reads and idempotent writes itself, so a resilience handler that retries too multiplies
    /// the attempts.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the context, API key and endpoint.</param>
    public static IHttpClientBuilder AddAgentMemory(
        this IServiceCollection services,
        Action<AgentMemoryOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = BuildOptions(configure);
        var builder = services.AddHttpClient(HttpClientNamePrefix, ConfigureClient);
        services.AddTransient(provider => new AgentMemoryClient(
            options,
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientNamePrefix)
        ));
        return builder;
    }

    /// <summary>
    /// Registers a keyed <see cref="AgentMemoryClient"/>, resolvable with <c>[FromKeyedServices(name)]</c> or
    /// <c>GetRequiredKeyedService&lt;AgentMemoryClient&gt;(name)</c>. Use it to talk to several contexts or
    /// deployments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key the client is registered under.</param>
    /// <param name="configure">Sets the context, API key and endpoint.</param>
    public static IHttpClientBuilder AddKeyedAgentMemory(
        this IServiceCollection services,
        string name,
        Action<AgentMemoryOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var options = BuildOptions(configure);
        var httpClientName = $"{HttpClientNamePrefix}:{name}";
        var builder = services.AddHttpClient(httpClientName, ConfigureClient);
        services.AddKeyedTransient(
            name,
            (provider, _) =>
                new AgentMemoryClient(
                    options,
                    provider.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName)
                )
        );
        return builder;
    }

    private static AgentMemoryOptions BuildOptions(Action<AgentMemoryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AgentMemoryOptions();
        configure(options);
        options.Validate();
        return options;
    }

    private static void ConfigureClient(HttpClient client)
    {
        // Deadlines are applied per request, so uploads are not cut off by a client-wide timeout.
        client.Timeout = Timeout.InfiniteTimeSpan;
    }
}
