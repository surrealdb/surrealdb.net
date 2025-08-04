using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurrealDb.Net;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register SurrealDB services.
/// Registers <see cref="ISurrealDbClient"/>.
/// Registers <see cref="IHttpClientFactory"/> for HTTP requests.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SurrealDB services from a ConnectionString.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Connection string to a SurrealDB instance.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal(
        this IServiceCollection services,
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        var configuration = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .Build();
        return AddSurreal(services, configuration, lifetime, configureCborOptions);
    }

    /// <summary>
    /// Registers SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">A delegate that is used to configure a <see cref="SurrealDbOptionsBuilder"/>.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal(
        this IServiceCollection services,
        Action<SurrealDbOptionsBuilder> configureOptions,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        var options = SurrealDbOptions.Create();
        configureOptions(options);
        return AddSurreal(services, options.Build(), lifetime, configureCborOptions);
    }

    /// <summary>
    /// Registers SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration options.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal(
        this IServiceCollection services,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        bool shouldRegisterHttpClient = new Uri(configuration.Endpoint).Scheme is "http" or "https";
        if (shouldRegisterHttpClient)
        {
            RegisterHttpClient(services, configuration.Endpoint);
        }

        services.AddSingleton<IValidateOptions<SurrealDbOptions>, SurrealDbOptionsValidation>();

        RegisterSurrealDbClient<ISurrealDbClient>(
            services,
            configuration,
            lifetime,
            configureCborOptions
        );
        RegisterSurrealDbClient<SurrealDbClient>(
            services,
            configuration,
            lifetime,
            configureCborOptions
        );

        return new SurrealDbBuilder(services);
    }

    /// <summary>
    /// Registers keyed SurrealDB services from a ConnectionString.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="connectionString">Connection string to a SurrealDB instance.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal(
        this IServiceCollection services,
        object? serviceKey,
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        var configuration = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .Build();

        return AddKeyedSurreal(services, serviceKey, configuration, lifetime, configureCborOptions);
    }

    /// <summary>
    /// Registers keyed SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureOptions">A delegate that is used to configure a <see cref="SurrealDbOptionsBuilder"/>.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal(
        this IServiceCollection services,
        object? serviceKey,
        Action<SurrealDbOptionsBuilder> configureOptions,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        var options = SurrealDbOptions.Create();
        configureOptions(options);
        return AddKeyedSurreal(
            services,
            serviceKey,
            options.Build(),
            lifetime,
            configureCborOptions
        );
    }

    /// <summary>
    /// Registers keyed SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configuration">Configuration options.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal(
        this IServiceCollection services,
        object? serviceKey,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        bool shouldRegisterHttpClient = new Uri(configuration.Endpoint).Scheme is "http" or "https";
        if (shouldRegisterHttpClient)
        {
            RegisterHttpClient(services, configuration.Endpoint);
        }

        RegisterKeyedSurrealDbClient<ISurrealDbClient>(
            services,
            serviceKey,
            configuration,
            lifetime,
            configureCborOptions
        );
        RegisterKeyedSurrealDbClient<SurrealDbClient>(
            services,
            serviceKey,
            configuration,
            lifetime,
            configureCborOptions
        );

        return new SurrealDbBuilder(services);
    }

    private static void RegisterHttpClient(IServiceCollection services, string endpoint)
    {
        var uri = new Uri(endpoint);
        string httpClientName = HttpClientHelper.GetHttpClientName(uri);

        services.AddHttpClient(
            httpClientName,
            client =>
            {
                client.BaseAddress = uri;
            }
        );
    }

    private static void RegisterSurrealDbClient<T>(
        IServiceCollection services,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton(
                    typeof(T),
                    serviceProvider =>
                        CreateSurrealDbClientWithoutPooling(
                            serviceProvider,
                            configuration,
                            configureCborOptions
                        )
                );
                break;
            case ServiceLifetime.Scoped:
                TryAddSingletonPoolContainer(services);
                services.AddScoped(
                    typeof(T),
                    serviceProvider =>
                        CreateSurrealDbClientWithPoolingFactory(
                            serviceProvider,
                            configuration,
                            configureCborOptions
                        )
                );
                break;
            case ServiceLifetime.Transient:
                TryAddSingletonPoolContainer(services);
                services.AddTransient(
                    typeof(T),
                    serviceProvider =>
                        CreateSurrealDbClientWithPoolingFactory(
                            serviceProvider,
                            configuration,
                            configureCborOptions
                        )
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(lifetime),
                    lifetime,
                    "Invalid service lifetime."
                );
        }
    }

    private static void RegisterKeyedSurrealDbClient<T>(
        IServiceCollection services,
        object? serviceKey,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddKeyedSingleton(
                    typeof(T),
                    serviceKey,
                    (serviceProvider, _) =>
                        CreateSurrealDbClientWithoutPooling(
                            serviceProvider,
                            configuration,
                            configureCborOptions
                        )
                );
                break;
            case ServiceLifetime.Scoped:
                TryAddSingletonPoolContainer(services);
                services.AddKeyedScoped(
                    typeof(T),
                    serviceKey,
                    (serviceProvider, _) =>
                        CreateSurrealDbClientWithPoolingFactory(
                            serviceProvider,
                            configuration,
                            configureCborOptions
                        )
                );
                break;
            case ServiceLifetime.Transient:
                TryAddSingletonPoolContainer(services);
                services.AddKeyedTransient(
                    typeof(T),
                    serviceKey,
                    (serviceProvider, _) =>
                        CreateSurrealDbClientWithPoolingFactory(
                            serviceProvider,
                            configuration,
                            configureCborOptions
                        )
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(lifetime),
                    lifetime,
                    "Invalid service lifetime."
                );
        }
    }

    private static SurrealDbClient CreateSurrealDbClientWithoutPooling(
        IServiceProvider serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions
    )
    {
        return new SurrealDbClient(
            configuration,
            serviceProvider,
            serviceProvider.GetService<IHttpClientFactory>(),
            configureCborOptions,
            serviceProvider.GetService<ILoggerFactory>()
        );
    }

    private static SurrealDbClient CreateSurrealDbClientWithPoolingFactory(
        IServiceProvider serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions
    )
    {
        bool supportsPooling = !configuration.IsEmbedded;
        if (!supportsPooling)
        {
            return CreateSurrealDbClientWithoutPooling(
                serviceProvider,
                configuration,
                configureCborOptions
            );
        }

        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
        var container = pool.TryGetExact(configuration.Endpoint);

        if (container is null)
        {
            return CreateSurrealDbClientWithoutPooling(
                serviceProvider,
                configuration,
                configureCborOptions
            );
        }

        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

        if (container.ClientEngine is not null)
        {
            return new SurrealDbClient(configuration, container.ClientEngine, poolTask);
        }

        var client = new SurrealDbClient(
            configuration,
            serviceProvider,
            serviceProvider.GetService<IHttpClientFactory>(),
            configureCborOptions,
            serviceProvider.GetService<ILoggerFactory>(),
            poolTask
        );
        container.ClientEngine = client.Engine;

        return client;
    }

    private static void TryAddSingletonPoolContainer(IServiceCollection services)
    {
        services.TryAddSingleton(_ =>
        {
            var policy = new AsyncPooledObjectPolicy<SurrealDbClientPoolContainer>();
            return new SurrealDbClientPool(policy);
        });
    }
}
