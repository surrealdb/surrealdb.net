using Microsoft.Extensions.Options;

namespace SurrealDB.NET.Http;

internal sealed class SurrealHttpClient : ISurrealHttpClient, IDisposable
{
	private readonly HttpClient _client;
	private SurrealOptions _options;
	private readonly IDisposable? _changeToken;

	public SurrealHttpClient(HttpClient client, IOptionsMonitor<SurrealOptions> options)
	{
		_client = client;
		_options = options.CurrentValue;
		_client.BaseAddress = new Uri($"{(_options.Secure ? "https" : "http")}://{_options.Endpoint.Host}:{_options.Endpoint.Port}");
		_changeToken = options.OnChange(o =>
		{
			_options = o;
			_client.BaseAddress = new Uri($"{(_options.Secure ? "https" : "http")}://{_options.Endpoint.Host}:{_options.Endpoint.Port}");
		});
	}

	public void Dispose()
	{
		_changeToken?.Dispose();
	}

	public Task ExportAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task<bool> HealthAsync(CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/health");

		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);

		return response.IsSuccessStatusCode;
	}

	public Task ImportAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<IEnumerable<T>> GetAllAsync<T>(Table table, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task DropAsync(Table table, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> GetAsync<T>(Thing thing, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task ModifyAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> DeleteAsync<T>(Thing thing, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SignupAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SigninNamespaceAsync<T>(string @namespace, T user, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task QueryAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<bool> StatusAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> VersionAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
