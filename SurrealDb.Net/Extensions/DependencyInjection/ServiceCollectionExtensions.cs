using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using SurrealDb.Net;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Internals.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register SurrealDB services.
/// Registers <see cref="ISurrealDbClient"/> as a singleton instance.
/// Registers <see cref="IHttpClientFactory"/> for HTTP requests.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SurrealDB services from a ConnectionString.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Connection string to a SurrealDB instance.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal(
        this IServiceCollection services,
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        return AddSurreal<ISurrealDbClient>(
            services,
            connectionString,
            lifetime,
            configureCborOptions
        );
    }

    /// <summary>
    /// Registers SurrealDB services from a ConnectionString.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="ISurrealDbClient"/> to register.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Connection string to a SurrealDB instance.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal<T>(
        this IServiceCollection services,
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
        where T : ISurrealDbClient
    {
        var configuration = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .Build();

        return AddSurreal<T>(services, configuration, lifetime, configureCborOptions);
    }

    /// <summary>
    /// Registers SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">A delegate that is used to configure a <see cref="SurrealDbOptionsBuilder"/>.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal(
        this IServiceCollection services,
        Action<SurrealDbOptionsBuilder> configureOptions,
        ServiceLifetime lifetime = ServiceLifetime.Singleton
    )
    {
        var options = SurrealDbOptions.Create();
        configureOptions(options);
        return AddSurreal<ISurrealDbClient>(services, options.Build(), lifetime);
    }

    /// <summary>
    /// Registers SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration options.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal(
        this IServiceCollection services,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        return AddSurreal<ISurrealDbClient>(
            services,
            configuration,
            lifetime,
            configureCborOptions
        );
    }

    /// <summary>
    /// Registers SurrealDB services with the specified configuration.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="ISurrealDbClient"/> to register.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration options.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddSurreal<T>(
        this IServiceCollection services,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
        where T : ISurrealDbClient
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        bool shouldRegisterHttpClient = new Uri(configuration.Endpoint).Scheme is "http" or "https";
        if (shouldRegisterHttpClient)
        {
            RegisterHttpClient(services, configuration.Endpoint);
        }

        services.AddSingleton<IValidateOptions<SurrealDbOptions>, SurrealDbOptionsValidation>();

        var classClientType = typeof(SurrealDbClient);
        var interfaceClientType = typeof(ISurrealDbClient);
        var type = typeof(T);

        bool isBaseType = type == classClientType || type == interfaceClientType;

        if (isBaseType)
        {
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
        }
        else
        {
            RegisterSurrealDbClient<T>(services, configuration, lifetime, configureCborOptions);
        }

        return new SurrealDbBuilder(services);
    }

    /// <summary>
    /// Registers keyed SurrealDB services from a ConnectionString.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="connectionString">Connection string to a SurrealDB instance.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal(
        this IServiceCollection services,
        object? serviceKey,
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        var configuration = SurrealDbOptions
            .Create()
            .FromConnectionString(connectionString)
            .Build();

        return AddKeyedSurreal<ISurrealDbClient>(
            services,
            serviceKey,
            configuration,
            lifetime,
            configureCborOptions
        );
    }

    /// <summary>
    /// Registers keyed SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configureOptions">A delegate that is used to configure a <see cref="SurrealDbOptionsBuilder"/>.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal(
        this IServiceCollection services,
        object? serviceKey,
        Action<SurrealDbOptionsBuilder> configureOptions,
        ServiceLifetime lifetime = ServiceLifetime.Singleton
    )
    {
        var options = SurrealDbOptions.Create();
        configureOptions(options);
        return AddKeyedSurreal<ISurrealDbClient>(services, serviceKey, options.Build(), lifetime);
    }

    /// <summary>
    /// Registers keyed SurrealDB services with the specified configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configuration">Configuration options.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal(
        this IServiceCollection services,
        object? serviceKey,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        return AddKeyedSurreal<ISurrealDbClient>(
            services,
            serviceKey,
            configuration,
            lifetime,
            configureCborOptions
        );
    }

    /// <summary>
    /// Registers keyed SurrealDB services with the specified configuration.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="ISurrealDbClient"/> to register.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <param name="configuration">Configuration options.</param>
    /// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <returns>Service collection</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static SurrealDbBuilder AddKeyedSurreal<T>(
        this IServiceCollection services,
        object? serviceKey,
        SurrealDbOptions configuration,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        Action<CborOptions>? configureCborOptions = null
    )
        where T : ISurrealDbClient
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        bool shouldRegisterHttpClient = new Uri(configuration.Endpoint).Scheme is "http" or "https";
        if (shouldRegisterHttpClient)
        {
            RegisterHttpClient(services, configuration.Endpoint);
        }

        var classClientType = typeof(SurrealDbClient);
        var interfaceClientType = typeof(ISurrealDbClient);
        var type = typeof(T);

        bool isBaseType = type == classClientType || type == interfaceClientType;

        if (isBaseType)
        {
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
        }
        else
        {
            RegisterKeyedSurrealDbClient<T>(
                services,
                serviceKey,
                configuration,
                lifetime,
                configureCborOptions
            );
        }

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
        services.TryAddSingleton(serviceProvider =>
        {
            var policy = new AsyncPooledObjectPolicy<SurrealDbClientPoolContainer>();
            return new SurrealDbClientPool(policy);
        });

        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton(
                    typeof(T),
                    serviceProvider =>
                    {
                        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
                        var container = pool.Get();

                        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

                        if (container.ClientEngine is not null)
                        {
                            return new SurrealDbClient(
                                configuration,
                                container.ClientEngine,
                                poolTask
                            );
                        }

                        return new SurrealDbClient(
                            configuration,
                            serviceProvider,
                            serviceProvider.GetService<IHttpClientFactory>(),
                            configureCborOptions,
                            poolTask,
                            serviceProvider.GetService<ILoggerFactory>()
                        );
                    }
                );
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped(
                    typeof(T),
                    serviceProvider =>
                    {
                        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
                        var container = pool.Get();

                        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

                        if (container.ClientEngine is not null)
                        {
                            return new SurrealDbClient(
                                configuration,
                                container.ClientEngine,
                                poolTask
                            );
                        }

                        var client = new SurrealDbClient(
                            configuration,
                            serviceProvider,
                            serviceProvider.GetService<IHttpClientFactory>(),
                            configureCborOptions,
                            poolTask,
                            serviceProvider.GetService<ILoggerFactory>()
                        );

                        container.ClientEngine = client.Engine;

                        return client;
                    }
                );
                break;
            case ServiceLifetime.Transient:
                services.AddTransient(
                    typeof(T),
                    serviceProvider =>
                    {
                        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
                        var container = pool.Get();

                        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

                        if (container.ClientEngine is not null)
                        {
                            return new SurrealDbClient(
                                configuration,
                                container.ClientEngine,
                                poolTask
                            );
                        }

                        return new SurrealDbClient(
                            configuration,
                            serviceProvider,
                            serviceProvider.GetService<IHttpClientFactory>(),
                            configureCborOptions,
                            poolTask,
                            serviceProvider.GetService<ILoggerFactory>()
                        );
                    }
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
                    {
                        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
                        var container = pool.Get();

                        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

                        if (container.ClientEngine is not null)
                        {
                            return new SurrealDbClient(
                                configuration,
                                container.ClientEngine,
                                poolTask
                            );
                        }

                        return new SurrealDbClient(
                            configuration,
                            serviceProvider,
                            serviceProvider.GetRequiredService<IHttpClientFactory>(),
                            configureCborOptions,
                            poolTask,
                            serviceProvider.GetService<ILoggerFactory>()
                        );
                    }
                );
                break;
            case ServiceLifetime.Scoped:
                services.AddKeyedScoped(
                    typeof(T),
                    serviceKey,
                    (serviceProvider, _) =>
                    {
                        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
                        var container = pool.Get();

                        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

                        if (container.ClientEngine is not null)
                        {
                            return new SurrealDbClient(
                                configuration,
                                container.ClientEngine,
                                poolTask
                            );
                        }

                        return new SurrealDbClient(
                            configuration,
                            serviceProvider,
                            serviceProvider.GetRequiredService<IHttpClientFactory>(),
                            configureCborOptions,
                            poolTask,
                            serviceProvider.GetService<ILoggerFactory>()
                        );
                    }
                );
                break;
            case ServiceLifetime.Transient:
                services.AddKeyedTransient(
                    typeof(T),
                    serviceKey,
                    (serviceProvider, _) =>
                    {
                        var pool = serviceProvider.GetRequiredService<SurrealDbClientPool>();
                        var container = pool.Get();

                        var poolTask = new Func<Task>(() => pool.ReturnAsync(container));

                        if (container.ClientEngine is not null)
                        {
                            return new SurrealDbClient(
                                configuration,
                                container.ClientEngine,
                                poolTask
                            );
                        }

                        var client = new SurrealDbClient(
                            configuration,
                            serviceProvider,
                            serviceProvider.GetRequiredService<IHttpClientFactory>(),
                            configureCborOptions,
                            poolTask,
                            serviceProvider.GetService<ILoggerFactory>()
                        );
                        container.ClientEngine = client.Engine;

                        return client;
                    }
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
}
