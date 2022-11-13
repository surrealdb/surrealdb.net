using System;
using System.Net.Http.Headers;
using SurrealDatabase.Net.Constants;

namespace SurrealDatabase.Net.Extensions.HttpClient
{
	public static class SurrealDatabaseClientExtension
	{
		public static System.Net.Http.HttpClient AddSurrealDatabaseHeaders(
			this System.Net.Http.HttpClient httpClient, string baseAddress, string nameSpace, string database, string username, string password)
		{
			httpClient.BaseAddress = new Uri(baseAddress);
			var headers = httpClient.DefaultRequestHeaders;
			var authenticationString = $"{username}:{password}";
			var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authenticationString));
			headers.Add(SurrealDatabaseHeaderConstants.Accept.Item1,SurrealDatabaseHeaderConstants.Accept.Item2);
			headers.Add(SurrealDatabaseHeaderConstants.ContentType.Item1, SurrealDatabaseHeaderConstants.ContentType.Item2);
			headers.Add(SurrealDatabaseHeaderConstants.Namespace, nameSpace);
			headers.Add(SurrealDatabaseHeaderConstants.Database, database);
			headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

			return httpClient;
		}
	}
}
