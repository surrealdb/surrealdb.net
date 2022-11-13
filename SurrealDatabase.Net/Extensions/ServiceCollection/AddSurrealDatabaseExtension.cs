using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SurrealDatabase.Net.Factories.SurrealDatabase;
using SurrealDatabase.Net.Implementers;
using SurrealDatabase.Net.Interfaces;

namespace SurrealDatabase.Net.Extensions.ServiceCollection
{
	public static class AddSurrealDatabaseExtension
	{
		public static IHttpClientBuilder AddSurrealDatabaseClient(this IServiceCollection services,
			Action<System.Net.Http.HttpClient> configureClient) =>
			services.AddHttpClient<ISurrealDatabaseClient, SurrealDatabaseClient>((httpClient) =>
			{
				SurrealDatabaseClientFactory.ConfigureHttpClientCore(httpClient);
				configureClient(httpClient);
			});
	}
}
