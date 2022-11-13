using System;
using System.Net.Http;
using SurrealDatabase.Net.Extensions.HttpClient;
using SurrealDatabase.Net.Implementers;
using SurrealDatabase.Net.Interfaces;

namespace SurrealDatabase.Net.Factories.SurrealDatabase
{
	public static class SurrealDatabaseClientFactory
	{
			public static ISurrealDatabaseClient Create(string url,
				string nameSpace,
				string database,
				string username,
				string password)
			{
				var httpClient = new HttpClient()
				{
					// BaseAddress = new Uri(host);
				};
				ConfigureHttpClient(httpClient, nameSpace, database, username, password);

				return new SurrealDatabaseClient(httpClient);
			}

			internal static void ConfigureHttpClient(
				HttpClient httpClient, string nameSpace,string database,string username,string password)
			{
				ConfigureHttpClientCore(httpClient);
				httpClient.AddSurrealDatabaseHeaders(nameSpace,database,username,password);
			}

			internal static void ConfigureHttpClientCore(HttpClient httpClient)
			{
				httpClient.DefaultRequestHeaders.Accept.Clear();
			}
		}
}
