using Dahomey.Cbor;
using Microsoft.Extensions.Options;
using SurrealDb.Net;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Helpers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register SurrealDB services.
/// Registers <see cref="ISurrealDbClient"/> for <see cref="ServiceLifetime.Singleton"/> lifetime.
/// Registers <see cref="ISurrealDbSession"/> for <see cref="ServiceLifetime.Scoped"/> and <see cref="ServiceLifetime.Transient"/> lifetimes.
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
        services.RegisterInnerServices(configuration);

        if (lifetime == ServiceLifetime.Singleton)
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
            RegisterSurrealDbClient<ISurrealDbSession>(
                services,
                configuration,
                lifetime,
                configureCborOptions
            );
            RegisterSurrealDbClient<SurrealDbSession>(
                services,
                configuration,
                lifetime,
                configureCborOptions
            );
        }

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
        services.RegisterInnerServices(configuration);

        if (lifetime == ServiceLifetime.Singleton)
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
            RegisterKeyedSurrealDbClient<ISurrealDbSession>(
                services,
                serviceKey,
                configuration,
                lifetime,
                configureCborOptions
            );
            RegisterKeyedSurrealDbClient<SurrealDbSession>(
                services,
                serviceKey,
                configuration,
                lifetime,
                configureCborOptions
            );
        }

        return new SurrealDbBuilder(services);
    }

    private static void RegisterInnerServices(
        this IServiceCollection services,
        SurrealDbOptions configuration
    )
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        bool shouldRegisterHttpClient = new Uri(configuration.Endpoint).Scheme is "http" or "https";
        if (shouldRegisterHttpClient)
        {
            services.RegisterHttpClient(configuration.Endpoint);
        }

        services.AddSingleton<IValidateOptions<SurrealDbOptions>, SurrealDbOptionsValidation>();
        services.AddSingleton<ISessionizer, Sessionizer>();
        services.AddKeyedSingleton<ISessionInfoProvider, RpcSessionInfoProvider>("http");
        services.AddKeyedSingleton<ISessionInfoProvider, RpcSessionInfoProvider>("https");
        services.AddKeyedSingleton<ISessionInfoProvider, RpcSessionInfoProvider>("ws");
        services.AddKeyedSingleton<ISessionInfoProvider, RpcSessionInfoProvider>("wss");
    }

    private static void RegisterHttpClient(this IServiceCollection services, string endpoint)
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
                        SurrealDbClientFactory.CreateSurrealDbClient(
                            serviceProvider,
                            configuration,
                            configureCborOptions,
                            null
                        )
                );
                break;
            case ServiceLifetime.Scoped:
                {
                    var surrealDbClientFactory = new SurrealDbClientFactory();

                    services.AddScoped(
                        typeof(T),
                        (serviceProvider) =>
                        {
                            var sessionizer = serviceProvider.GetRequiredService<ISessionizer>();
                            var session = surrealDbClientFactory.CreateChildClient(
                                serviceProvider,
                                configuration,
                                configureCborOptions,
                                sessionizer
                            );

                            var sessionInfoProvider =
                                serviceProvider.GetRequiredKeyedService<ISessionInfoProvider>(
                                    session.Uri.Scheme
                                );
                            sessionizer.Add(
                                session.SessionId!.Value,
                                sessionInfoProvider.Get(configuration)
                            );

                            return session;
                        }
                    );
                }
                break;
            case ServiceLifetime.Transient:
                {
                    var surrealDbClientFactory = new SurrealDbClientFactory();

                    services.AddTransient(
                        typeof(T),
                        (serviceProvider) =>
                        {
                            var sessionizer = serviceProvider.GetRequiredService<ISessionizer>();
                            var session = surrealDbClientFactory.CreateChildClient(
                                serviceProvider,
                                configuration,
                                configureCborOptions,
                                sessionizer
                            );

                            var sessionInfoProvider =
                                serviceProvider.GetRequiredKeyedService<ISessionInfoProvider>(
                                    session.Uri.Scheme
                                );
                            sessionizer.Add(
                                session.SessionId!.Value,
                                sessionInfoProvider.Get(configuration)
                            );

                            return session;
                        }
                    );
                }
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
                        SurrealDbClientFactory.CreateSurrealDbClient(
                            serviceProvider,
                            configuration,
                            configureCborOptions,
                            null
                        )
                );
                break;
            case ServiceLifetime.Scoped:
                {
                    var surrealDbClientFactory = new SurrealDbClientFactory();

                    services.AddKeyedScoped(
                        typeof(T),
                        serviceKey,
                        (serviceProvider, _) =>
                        {
                            var sessionizer = serviceProvider.GetRequiredService<ISessionizer>();
                            var session = surrealDbClientFactory.CreateChildClient(
                                serviceProvider,
                                configuration,
                                configureCborOptions,
                                sessionizer
                            );

                            var sessionInfoProvider =
                                serviceProvider.GetRequiredKeyedService<ISessionInfoProvider>(
                                    session.Uri.Scheme
                                );
                            sessionizer.Add(
                                session.SessionId!.Value,
                                sessionInfoProvider.Get(configuration)
                            );

                            return session;
                        }
                    );
                }
                break;
            case ServiceLifetime.Transient:
                {
                    var surrealDbClientFactory = new SurrealDbClientFactory();

                    services.AddKeyedTransient(
                        typeof(T),
                        serviceKey,
                        (serviceProvider, _) =>
                        {
                            var sessionizer = serviceProvider.GetRequiredService<ISessionizer>();
                            var session = surrealDbClientFactory.CreateChildClient(
                                serviceProvider,
                                configuration,
                                configureCborOptions,
                                sessionizer
                            );

                            var sessionInfoProvider =
                                serviceProvider.GetRequiredKeyedService<ISessionInfoProvider>(
                                    session.Uri.Scheme
                                );
                            sessionizer.Add(
                                session.SessionId!.Value,
                                sessionInfoProvider.Get(configuration)
                            );

                            return session;
                        }
                    );
                }
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
