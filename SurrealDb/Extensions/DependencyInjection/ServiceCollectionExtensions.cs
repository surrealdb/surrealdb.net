using SurrealDb;
using SurrealDb.Internals.Helpers;

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
	/// <returns>Service collection</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddSurreal(
		this IServiceCollection services,
		string connectionString,
		ServiceLifetime lifetime = ServiceLifetime.Singleton
	)
	{
		return AddSurreal<ISurrealDbClient>(services, connectionString, lifetime);
	}

	/// <summary>
	/// Registers SurrealDB services from a ConnectionString.
	/// </summary>
	/// <typeparam name="T">Type of <see cref="ISurrealDbClient"/> to register.</typeparam>
	/// <param name="services">Service collection.</param>
	/// <param name="connectionString">Connection string to a SurrealDB instance.</param>
	/// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
	/// <returns>Service collection</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddSurreal<T>(
		this IServiceCollection services,
		string connectionString,
		ServiceLifetime lifetime = ServiceLifetime.Singleton
	) where T : ISurrealDbClient
	{
		var configuration = SurrealDbOptions
			.Create()
			.FromConnectionString(connectionString)
			.Build();

		return AddSurreal<T>(services, configuration, lifetime);
	}

	/// <summary>
	/// Registers SurrealDB services with the specified configuration.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <param name="configuration">Configuration options.</param>
	/// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
	/// <returns>Service collection</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddSurreal(
		this IServiceCollection services,
		SurrealDbOptions configuration,
		ServiceLifetime lifetime = ServiceLifetime.Singleton
	)
	{
		return AddSurreal<ISurrealDbClient>(services, configuration, lifetime);
	}

	/// <summary>
	/// Registers SurrealDB services with the specified configuration.
	/// </summary>
	/// <typeparam name="T">Type of <see cref="ISurrealDbClient"/> to register.</typeparam>
	/// <param name="services">Service collection.</param>
	/// <param name="configuration">Configuration options.</param>
	/// <param name="lifetime">Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Singleton"/>.</param>
	/// <returns>Service collection</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddSurreal<T>(
		this IServiceCollection services,
		SurrealDbOptions configuration,
		ServiceLifetime lifetime = ServiceLifetime.Singleton
	) where T : ISurrealDbClient
	{
		if (configuration.Endpoint is null)
			throw new ArgumentNullException(nameof(configuration));

		RegisterHttpClient(services, configuration.Endpoint);

		var classClientType = typeof(SurrealDbClient);
		var interfaceClientType = typeof(ISurrealDbClient);
		var type = typeof(T);

		bool isBaseType = type == classClientType || type == interfaceClientType;

		if (isBaseType)
		{
			RegisterSurrealDbClient<ISurrealDbClient>(services, configuration, lifetime);
			RegisterSurrealDbClient<SurrealDbClient>(services, configuration, lifetime);
		}
		else
		{
			RegisterSurrealDbClient<T>(services, configuration, lifetime);
		}

		return services;
	}

	private static void RegisterHttpClient(IServiceCollection services, string endpoint)
	{
		var uri = new Uri(endpoint);
		string httpClientName = HttpClientHelper.GetHttpClientName(uri);

		services.AddHttpClient(httpClientName, client =>
		{
			client.BaseAddress = uri;
		});
	}

	private static void RegisterSurrealDbClient<T>(IServiceCollection services, SurrealDbOptions configuration, ServiceLifetime lifetime)
	{
		switch (lifetime)
		{
			case ServiceLifetime.Singleton:
				services.AddSingleton(typeof(T), serviceProvider =>
				{
					return new SurrealDbClient(configuration, serviceProvider.GetRequiredService<IHttpClientFactory>());
				});
				break;
			case ServiceLifetime.Scoped:
				services.AddScoped(typeof(T), serviceProvider =>
				{
					return new SurrealDbClient(configuration, serviceProvider.GetRequiredService<IHttpClientFactory>());
				});
				break;
			case ServiceLifetime.Transient:
				services.AddTransient(typeof(T), serviceProvider =>
				{
					return new SurrealDbClient(configuration, serviceProvider.GetRequiredService<IHttpClientFactory>());
				});
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
		}
	}
}
