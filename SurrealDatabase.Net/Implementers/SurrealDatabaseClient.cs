using System.Net.Http;
using SurrealDatabase.Net.Interfaces;

namespace SurrealDatabase.Net.Implementers
{
	public class SurrealDatabaseClient : ISurrealDatabaseClient
	{
		private readonly HttpClient httpClient;

		public SurrealDatabaseClient(HttpClient httpClient) =>
			this.httpClient = httpClient;
	}
}
