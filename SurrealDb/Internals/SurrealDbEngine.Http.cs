using SurrealDb.Exceptions;
using SurrealDb.Internals.Auth;
using SurrealDb.Internals.Constants;
using SurrealDb.Internals.Helpers;
using SurrealDb.Internals.Json;
using SurrealDb.Internals.Models;
using SurrealDb.Models;
using SurrealDb.Models.Auth;
using SurrealDb.Models.Response;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SurrealDb.Internals;

internal class SurrealDbHttpEngine : ISurrealDbEngine
{
    private readonly Uri _uri;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly SurrealDbEngineConfig _config = new();

    public SurrealDbHttpEngine(Uri uri, IHttpClientFactory? httpClientFactory)
    {
        _uri = uri;
        _httpClientFactory = httpClientFactory;
    }

    public async Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.BEARER, jwt.Token);

		using var body = CreateBodyContent("RETURN TRUE");

		using var response = await client.PostAsync("/sql", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);
		EnsuresFirstResultOk(dbResponse);

		_config.SetBearerAuth(jwt.Token);
	}

	public void Configure(string? ns, string? db, string? username, string? password)
	{
		if (ns is not null)
			_config.Use(ns, db);

		if (username is not null)
			_config.SetBasicAuth(username, password);
	}

	public async Task Connect(CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();
		using var body = CreateBodyContent("RETURN TRUE");

		using var response = await client.PostAsync("/sql", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);
		EnsuresFirstResultOk(dbResponse);
	}

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken) where T : Record
    {
        using var client = CreateHttpClient();
		using var body = CreateBodyContent(data);

		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		using var response = await client.PostAsync($"/key/{data.Id.Table}/{data.Id.Id}", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<T>>(dbResponse)!;
		return list.First();
	}
    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(data);

		using var response = await client.PostAsync($"/key/{table}", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<T>>(dbResponse)!;
		return list.First();
	}

	public async Task Delete(string table, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

		using var response = await client.DeleteAsync($"/key/{table}", cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);
		EnsuresFirstResultOk(dbResponse);
	}

	public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

		using var response = await client.DeleteAsync($"/key/{thing.Table}/{thing.Id}", cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<object>>(dbResponse)!;
		return list.Any(r => r is not null);
    }

    public void Invalidate()
    {
        _config.ResetAuth();
    }

	public async Task<TOutput> Patch<TPatch, TOutput>(TPatch data, CancellationToken cancellationToken) where TPatch : Record
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(data);

		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		using var response = await client.PatchAsync($"/key/{data.Id.Table}/{data.Id.Id}", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<TOutput>>(dbResponse)!;
		return list.First();
	}
	public async Task<T> Patch<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken)
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(data);

		using var response = await client.PatchAsync($"/key/{thing.Table}/{thing.Id}", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<T>>(dbResponse)!;
		return list.First();
	}

	public async Task<SurrealDbResponse> Query(
		string query,
		IReadOnlyDictionary<string, object> parameters,
		CancellationToken cancellationToken
	)
    {
        using var client = CreateHttpClient();
		using var body = CreateBodyContent(query);

		var queryString = HttpUtility.ParseQueryString(string.Empty);

		foreach (var (key, value) in _config.Parameters)
		{
			queryString[key] = JsonSerializer.Serialize(value, SurrealDbSerializerOptions.Default);
		}
		foreach (var (key, value) in parameters)
		{
			queryString[key] = JsonSerializer.Serialize(value, SurrealDbSerializerOptions.Default);
		}

		var uriBuilder = new UriBuilder
		{
			Scheme = string.Empty,
			Host = string.Empty,
			Path = "/sql",
			Query = queryString.ToString()
		};
		var requestUri = uriBuilder.ToString();

		using var response = await client.PostAsync(requestUri, body, cancellationToken);

		return await DeserializeDbResponseAsync(response, cancellationToken);
	}

	public async Task<List<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

		using var response = await client.GetAsync($"/key/{table}", cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);
		return ExtractFirstResultValue<List<T>>(dbResponse)!;
	}
    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

		using var response = await client.GetAsync($"/key/{thing.Table}/{thing.Id}", cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<T>>(dbResponse)!;
		return list.FirstOrDefault();
    }

    public async Task Set(string key, object value, CancellationToken cancellationToken)
    {
        var dbResponse = await Query(
            $"RETURN ${key}", 
            new Dictionary<string, object>() { { key, value } }, 
            cancellationToken
        );
		EnsuresFirstResultOk(dbResponse);

		_config.SetParam(key, value);
    }

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();
		using var body = CreateBodyContent(rootAuth);

		using var response = await client.PostAsync("/signin", body, cancellationToken);
        response.EnsureSuccessStatusCode();

        _config.SetBasicAuth(rootAuth.Username, rootAuth.Password);
    }
	public async Task SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(nsAuth);

		using var response = await client.PostAsync("/signin", body, cancellationToken);
		response.EnsureSuccessStatusCode();

		_config.SetBasicAuth(nsAuth.Username, nsAuth.Password);
	}
	public async Task SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(dbAuth);

		using var response = await client.PostAsync("/signin", body, cancellationToken);
		response.EnsureSuccessStatusCode();

		_config.SetBasicAuth(dbAuth.Username, dbAuth.Password);
	}
	public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(scopeAuth);

		using var response = await client.PostAsync("/signin", body, cancellationToken);
		response.EnsureSuccessStatusCode();

		var result = await DeserializeAuthResponse(response, cancellationToken);

		_config.SetBearerAuth(result.Token!);

		return new Jwt { Token = result.Token! };
	}

	public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(scopeAuth);

		using var response = await client.PostAsync("/signup", body, cancellationToken);
		response.EnsureSuccessStatusCode();

		var result = await DeserializeAuthResponse(response, cancellationToken);

		return new Jwt { Token = result.Token! };
	}

	public void Unset(string key)
    {
        _config.RemoveParam(key);
    }

	public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken) where T : Record
	{
		using var client = CreateHttpClient();
		using var body = CreateBodyContent(data);

		if (data.Id is null)
			throw new SurrealDbException("Cannot create a record without an Id");

		using var response = await client.PutAsync($"/key/{data.Id.Table}/{data.Id.Id}", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);

		var list = ExtractFirstResultValue<List<T>>(dbResponse)!;
		return list.First();
	}

	public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Add("NS", ns);
        client.DefaultRequestHeaders.Add("DB", db);

		using var body = CreateBodyContent("RETURN TRUE");

		using var response = await client.PostAsync("/sql", body, cancellationToken);

		var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken);
		EnsuresFirstResultOk(dbResponse);

		_config.Use(ns, db);
    }

    internal HttpClient CreateHttpClient(bool withAuth = true)
    {
        var client = GetHttpClient();
        client.BaseAddress = _uri;

        client.DefaultRequestHeaders.Add("Accept", new[] { "application/json" });
        client.DefaultRequestHeaders.Add("NS", _config.Ns);
        client.DefaultRequestHeaders.Add("DB", _config.Db);

        if (withAuth)
        {
            if (_config.Auth is BearerAuth bearerAuth)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.BEARER, bearerAuth.Token);
            }
            if (_config.Auth is BasicAuth basicAuth)
            {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{basicAuth.Username}:{basicAuth.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.BASIC, credentials);
			}
		}
        
        return client;
    }

    private HttpClient GetHttpClient()
    {
		if (_httpClientFactory is not null)
		{
			string httpClientName = HttpClientHelper.GetHttpClientName(_uri);
			return _httpClientFactory.CreateClient(httpClientName);
		}

        return new HttpClient();
	}

	private static StringContent CreateBodyContent<T>(T data)
	{
		string bodyContent = data is string str
			? str
			: JsonSerializer.Serialize(data, SurrealDbSerializerOptions.Default);

		return new StringContent(bodyContent, Encoding.UTF8, "application/json");
	}

	private static async Task<SurrealDbResponse> DeserializeDbResponseAsync(
		HttpResponseMessage response,
		CancellationToken cancellationToken
	)
	{
#if NET6_0_OR_GREATER
		using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
        using var stream = await response.Content.ReadAsStreamAsync();
#endif

		if (!response.IsSuccessStatusCode)
		{
			var result = await JsonSerializer.DeserializeAsync<ISurrealDbResult>(stream, SurrealDbSerializerOptions.Default, cancellationToken);
			return new SurrealDbResponse(result!);
		}

		var list = new List<ISurrealDbResult>();

		await foreach (var result in JsonSerializer.DeserializeAsyncEnumerable<ISurrealDbResult>(stream, SurrealDbSerializerOptions.Default, cancellationToken))
		{
			if (result is not null)
				list.Add(result);
		}

		return new SurrealDbResponse(list);
	}

	private async Task<AuthResponse> DeserializeAuthResponse(
		HttpResponseMessage response,
		CancellationToken cancellationToken
	)
	{
#if NET6_0_OR_GREATER
		using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
		using var stream = await response.Content.ReadAsStreamAsync();
#endif

		var authResponse = await JsonSerializer.DeserializeAsync<AuthResponse>(
			stream,
			SurrealDbSerializerOptions.Default,
			cancellationToken
		);

		if (authResponse is null)
			throw new SurrealDbException("Cannot deserialize auth response");

		return authResponse;
	}

	private static SurrealDbOkResult EnsuresFirstResultOk(SurrealDbResponse dbResponse)
	{
		if (dbResponse.IsEmpty)
			throw new EmptySurrealDbResponseException();

		var firstResult = dbResponse.FirstResult ?? throw new SurrealDbErrorResultException();

		if (firstResult is ISurrealDbErrorResult errorResult)
			throw new SurrealDbErrorResultException(errorResult);

		return (SurrealDbOkResult)firstResult;
	}
	private static T? ExtractFirstResultValue<T>(SurrealDbResponse dbResponse)
	{
		var okResult = EnsuresFirstResultOk(dbResponse);
		return okResult.GetValue<T>();
	}
}
