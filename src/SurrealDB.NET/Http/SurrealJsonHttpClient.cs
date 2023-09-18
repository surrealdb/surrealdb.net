using Microsoft.Extensions.Options;
using SurrealDB.NET.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace SurrealDB.NET.Http;

internal sealed class SurrealJsonHttpClient : ISurrealHttpClient, IDisposable
{
	private readonly HttpClient _client;
	private SurrealOptions _options;
	private readonly IDisposable? _changeToken;

	public SurrealJsonHttpClient(HttpClient client, IOptionsMonitor<SurrealOptions> options)
	{
		_client = client;
		_options = options.CurrentValue;
		_client.BaseAddress = new Uri($"{(_options.Secure ? "https" : "http")}://{_options.Endpoint.Host}:{_options.Endpoint.Port}");
		_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_client.DefaultRequestHeaders.Add("NS", _options.DefaultNamespace);
		_client.DefaultRequestHeaders.Add("DB", _options.DefaultDatabase);
		_changeToken = options.OnChange(o =>
		{
			_options = o;
			_client.BaseAddress = new Uri($"{(_options.Secure ? "https" : "http")}://{_options.Endpoint.Host}:{_options.Endpoint.Port}");
			_client.DefaultRequestHeaders.Add("NS", _options.DefaultNamespace);
			_client.DefaultRequestHeaders.Add("DB", _options.DefaultDatabase);
		});
	}

	public void Dispose()
	{
		_changeToken?.Dispose();
	}

	public void AttachToken(string token)
	{
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
	}

	public async Task ExportAsync(Stream destination, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, "/export");
		request.Headers.Accept.Clear();
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

		await source.CopyToAsync(destination, ct).ConfigureAwait(false);
	}

	private static readonly Uri _healthUri = new("/health", UriKind.Relative);

	public async Task<bool> HealthAsync(CancellationToken ct = default)
	{
		using var response = await _client.GetAsync(_healthUri, ct).ConfigureAwait(false);
		return response.IsSuccessStatusCode;
	}

	private static readonly Uri _importUri = new("/import", UriKind.Relative);

	public async Task ImportAsync(Stream source, CancellationToken ct = default)
	{
		using var content = new StreamContent(source);
		using var response = await _client.PostAsync(_importUri, content, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
	}

	public async Task<IEnumerable<T>> GetAllAsync<T>(Table table, CancellationToken ct = default)
	{
		var element = await _client.GetFromJsonAsync<JsonElement>($"/key/{table.Name}", ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return element.EnumerateArray().First().GetProperty("result"u8).Deserialize<IEnumerable<T>>() ?? throw new SurrealException("Can not deserialize JSON null to IEnumerable<T>");
	}

	public async Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default)
	{
		using var response = await _client.PostAsJsonAsync($"/key/{table.Name}", data, _options.JsonRequestOptions, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var element = await response.Content.ReadFromJsonAsync<JsonElement>(_options.JsonResponseOptions, ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return element.EnumerateArray().First().GetProperty("result"u8).EnumerateArray().First().Deserialize<T>()!;
	}

	public async Task<IEnumerable<T>> DropAsync<T>(Table table, CancellationToken ct = default)
	{
		return await _client.DeleteFromJsonAsync<IEnumerable<T>>($"key/{table.Name}", ct).ConfigureAwait(false)
			?? throw new SurrealException("Response was JSON null");
	}

	public async Task<T?> GetAsync<T>(Thing thing, CancellationToken ct = default)
	{
		var element = await _client.GetFromJsonAsync<JsonElement>($"key/{thing.Table.Name}/{thing.Id}", ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return element.EnumerateArray().First().GetProperty("result"u8).EnumerateArray().First().Deserialize<T>();
	}

	public async Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default)
	{
		if (!thing.IsSpecific)
			return await InsertAsync(thing.Table, data, ct).ConfigureAwait(false);

		using var response = await _client.PostAsJsonAsync($"key/{thing.Table.Name}/{thing.Id}", data, _options.JsonRequestOptions, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var element = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return element.EnumerateArray().First().GetProperty("result"u8).EnumerateArray().First().Deserialize<T>()!;
	}

	public async Task<T?> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default)
	{
		if (!thing.IsSpecific)
			throw new SurrealException("SurrealDB.NET can only update single records over HTTP");

		using var response = await _client.PutAsJsonAsync($"key/{thing.Table.Name}/{thing.Id}", data, _options.JsonRequestOptions, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var element = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return element.EnumerateArray().First().GetProperty("result"u8).EnumerateArray().First().Deserialize<T>();
	}

	public async Task<T?> MergeAsync<T>(Thing thing, object merge, CancellationToken ct = default)
	{
		if (!thing.IsSpecific)
			throw new SurrealException("SurrealDB.NET can only merge into single records over HTTP");

		using var response = await _client.PatchAsJsonAsync($"key/{thing.Table.Name}/{thing.Id}", merge, _options.JsonRequestOptions, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var element = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return element.EnumerateArray().First().GetProperty("result"u8).EnumerateArray().First().Deserialize<T>();
	}

	public async Task<T?> DeleteAsync<T>(Thing thing, CancellationToken ct = default)
	{
		var results =  await _client.DeleteFromJsonAsync<IEnumerable<SurrealQueryResultPage>>($"key/{thing.Table.Name}/{thing.Id}", _options.JsonResponseOptions, ct).ConfigureAwait(false);

		if (results is null)
			return default;

		var innerResults = results.FirstOrDefault().Result.Deserialize<IEnumerable<T>>();

		if (innerResults is null)
			return default;

		return innerResults.FirstOrDefault();
	}

	public async Task<string> SignupAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/signup");
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);

		writer.WriteStartObject();
		writer.WriteString("ns"u8, @namespace);
		writer.WriteString("db"u8, database);
		writer.WriteString("sc"u8, scope);

		var ext = JsonSerializer.SerializeToElement(user, _options.JsonRequestOptions);
		foreach (var prop in ext.EnumerateObject())
		{
			writer.WritePropertyName(prop.Name);
			prop.Value.WriteTo(writer);
		}

		writer.WriteEndObject();

		await writer.FlushAsync(ct).ConfigureAwait(false);
		stream.Position = 0;

		request.Content = new StreamContent(stream);

		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		var element = SurrealJson.BytesToJsonElement(responseStream);
		SurrealJson.ThrowOnError(element);
		return element.GetProperty("token"u8).GetString() ?? throw new SurrealException("Token not found");
	}

	public async Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/signin");
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);

		writer.WriteStartObject();
		writer.WriteString("ns"u8, @namespace);
		writer.WriteString("db"u8, database);
		writer.WriteString("sc"u8, scope);

		var ext = JsonSerializer.SerializeToElement(user, _options.JsonRequestOptions);
		foreach (var prop in ext.EnumerateObject())
		{
			writer.WritePropertyName(prop.Name);
			prop.Value.WriteTo(writer);
		}

		writer.WriteEndObject();

		await writer.FlushAsync(ct).ConfigureAwait(false);
		stream.Position = 0;

		request.Content = new StreamContent(stream);

		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		var element = SurrealJson.BytesToJsonElement(responseStream);
		SurrealJson.ThrowOnError(element);
		return element.GetProperty("token"u8).GetString() ?? throw new SurrealException("Token not found");
	}

	public async Task<string> SigninNamespaceAsync(string @namespace, string username, string password, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/signin");
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);

		writer.WriteStartObject();
		writer.WriteString("ns"u8, @namespace);
		writer.WriteString("user"u8, username);
		writer.WriteString("pass"u8, password);
		writer.WriteEndObject();

		await writer.FlushAsync(ct).ConfigureAwait(false);
		stream.Position = 0;

		request.Content = new StreamContent(stream);

		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		var element = SurrealJson.BytesToJsonElement(responseStream);
		SurrealJson.ThrowOnError(element);
		return element.GetProperty("token"u8).GetString() ?? throw new SurrealException("Token not found");
	}

	public async Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/signin");
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);

		writer.WriteStartObject();
		writer.WriteString("user"u8, username);
		writer.WriteString("pass"u8, password);
		writer.WriteEndObject();

		await writer.FlushAsync(ct).ConfigureAwait(false);
		stream.Position = 0;

		request.Content = new StreamContent(stream);

		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		var element = SurrealJson.BytesToJsonElement(responseStream);
		SurrealJson.ThrowOnError(element);
		return element.GetProperty("token"u8).GetString() ?? throw new SurrealException("Token not found");
	}

	public async Task<SurrealQueryResult> QueryAsync(string query, object? vars = null, CancellationToken ct = default)
	{
		var queryParameters = vars?.GetType().GetProperties().Select(p =>
		{
			var n = _options.JsonRequestOptions.PropertyNamingPolicy?.ConvertName(p.Name) ?? p.Name;
			var v = p.GetValue(vars);

			if (v is Thing thing)
				return (n, $"{thing.Table.Name}:{thing.Id}");

			if (v is Table table)
				return (n, table.Name);

			var encoded = HttpUtility.UrlEncode(p.GetValue(vars)?.ToString() ?? "");
			return (n, encoded);
		}).ToArray();

		using var request = new HttpRequestMessage(HttpMethod.Post, queryParameters switch
		{
			[] or null => "/sql",
			[var o] => $"/sql?{o.n}={o.Item2}",
			[var on, .. var rest] => $"/sql?{on.n}={on.Item2}{rest.Select(r => $"&{r.n}={r.Item2}")}",
		});

		request.Content = new StringContent(query);
		using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();
		var element = await response.Content.ReadFromJsonAsync<JsonElement>(ct).ConfigureAwait(false);
		SurrealJson.ThrowOnError(element);
		return new SurrealQueryResult(element);
	}

	private static readonly Uri _statusUri = new("/status", UriKind.Relative);

	public async Task<bool> StatusAsync(CancellationToken ct = default)
	{
		using var response = await _client.GetAsync(_statusUri, ct).ConfigureAwait(false);
		return response.IsSuccessStatusCode;
	}

	private static readonly Uri _versionUri = new("/version", UriKind.Relative);

	public async Task<string> VersionAsync(CancellationToken ct = default)
	{
		return await _client.GetStringAsync(_versionUri, ct).ConfigureAwait(false);
	}
}
