using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SurrealDb.Net.Internals;

internal class SurrealDbHttpEngine : ISurrealDbEngine
{
    private readonly Uri _uri;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly Action<JsonSerializerOptions>? _configureJsonSerializerOptions;
    private readonly Lazy<HttpClient> _singleHttpClient = new(() => new HttpClient(), true);
    private HttpClientConfiguration? _singleHttpClientConfiguration;
    private readonly SurrealDbHttpEngineConfig _config = new();

    public SurrealDbHttpEngine(
        Uri uri,
        IHttpClientFactory? httpClientFactory,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions
    )
    {
        _uri = uri;
        _httpClientFactory = httpClientFactory;
        _configureJsonSerializerOptions = configureJsonSerializerOptions;
    }

    public async Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper(new BearerAuth(jwt.Token));
        using var body = CreateBodyContent("RETURN TRUE");

        using var response = await wrapper.Instance
            .PostAsync("/sql", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);
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

    public void Configure(string? ns, string? db, string? token = null)
    {
        if (ns is not null)
            _config.Use(ns, db);

        if (token is not null)
            _config.SetBearerAuth(token);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent("RETURN TRUE");

        using var response = await wrapper.Instance
            .PostAsync("/sql", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);
        EnsuresFirstResultOk(dbResponse);
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(data);

        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        using var response = await wrapper.Instance
            .PostAsync($"/key/{data.Id.Table}/{data.Id.UnescapedId}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<T>().First();
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = data is null ? new StringContent("{}") : CreateBodyContent(data);

        using var response = await wrapper.Instance
            .PostAsync($"/key/{table}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<T>().First();
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();

        using var response = await wrapper.Instance
            .DeleteAsync($"/key/{table}", cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);
        EnsuresFirstResultOk(dbResponse);
    }

    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();

        using var response = await wrapper.Instance
            .DeleteAsync($"/key/{thing.Table}/{thing.UnescapedId}", cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<object>().Any(r => r is not null);
    }

    public void Dispose()
    {
        if (_singleHttpClient.IsValueCreated)
            _singleHttpClient.Value.Dispose();
    }

    public async Task<bool> Health(CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();

        try
        {
            using var response = await wrapper.Instance
                .GetAsync("/health", cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<T> Info<T>(CancellationToken cancellationToken)
    {
        const string query = "SELECT * FROM $auth;";

        var dbResponse = await Query(query, new Dictionary<string, object>(), cancellationToken)
            .ConfigureAwait(false);

        EnsuresFirstResultOk(dbResponse);

        var results = dbResponse.GetValue<List<T>>(0)!;
        return results.First();
    }

    public Task Invalidate(CancellationToken _)
    {
        _config.ResetAuth();
        return Task.CompletedTask;
    }

    public Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid)
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        string query,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public async Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : Record
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(data);

        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        using var response = await wrapper.Instance
            .PatchAsync($"/key/{data.Id.Table}/{data.Id.UnescapedId}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<TOutput>().First();
    }

    public async Task<T> Merge<T>(
        Thing thing,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(data);

        using var response = await wrapper.Instance
            .PatchAsync($"/key/{thing.Table}/{thing.UnescapedId}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<T>().First();
    }

    public async Task<IEnumerable<TOutput>> MergeAll<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(data);

        using var response = await wrapper.Instance
            .PatchAsync($"/key/{table}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        return ExtractFirstResultValue<List<TOutput>>(dbResponse)!; // TODO : .DeserializeEnumerable<T>
    }

    public async Task<IEnumerable<T>> MergeAll<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(data);

        using var response = await wrapper.Instance
            .PatchAsync($"/key/{table}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        return ExtractFirstResultValue<List<T>>(dbResponse)!; // TODO : .DeserializeEnumerable<T>
    }

    public async Task<SurrealDbResponse> Query(
        string query,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken
    )
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(query);

        var queryString = HttpUtility.ParseQueryString(string.Empty);

        var jsonSerializerOptions = GetJsonSerializerOptions();

        foreach (var (key, value) in _config.Parameters)
        {
            queryString[key] = JsonSerializer.Serialize(value, jsonSerializerOptions);
        }
        foreach (var (key, value) in parameters)
        {
            queryString[key] = JsonSerializer.Serialize(value, jsonSerializerOptions);
        }

        var uriBuilder = new UriBuilder
        {
            Scheme = string.Empty,
            Host = string.Empty,
            Path = "/sql",
            Query = queryString.ToString()
        };
        var requestUri = uriBuilder.ToString();

        using var response = await wrapper.Instance
            .PostAsync(requestUri, body, cancellationToken)
            .ConfigureAwait(false);

        return await DeserializeDbResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();

        using var response = await wrapper.Instance
            .GetAsync($"/key/{table}", cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<T>();
    }

    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();

        using var response = await wrapper.Instance
            .GetAsync($"/key/{thing.Table}/{thing.UnescapedId}", cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<T>().FirstOrDefault();
    }

    public async Task Set(string key, object value, CancellationToken cancellationToken)
    {
        var dbResponse = await Query(
                $"RETURN ${key}",
                new Dictionary<string, object>() { { key, value } },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsuresFirstResultOk(dbResponse);

        _config.SetParam(key, value);
    }

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(rootAuth);

        using var response = await wrapper.Instance
            .PostAsync("/signin", body, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        _config.SetBasicAuth(rootAuth.Username, rootAuth.Password);
    }

    public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(nsAuth);

        using var response = await wrapper.Instance
            .PostAsync("/signin", body, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await DeserializeAuthResponse(response, cancellationToken)
            .ConfigureAwait(false);

        _config.SetBearerAuth(result.Token!);

        return new Jwt { Token = result.Token! };
    }

    public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(dbAuth);

        using var response = await wrapper.Instance
            .PostAsync("/signin", body, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await DeserializeAuthResponse(response, cancellationToken)
            .ConfigureAwait(false);

        _config.SetBearerAuth(result.Token!);

        return new Jwt { Token = result.Token! };
    }

    public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(scopeAuth);

        using var response = await wrapper.Instance
            .PostAsync("/signin", body, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await DeserializeAuthResponse(response, cancellationToken)
            .ConfigureAwait(false);

        _config.SetBearerAuth(result.Token!);

        return new Jwt { Token = result.Token! };
    }

    public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(scopeAuth);

        using var response = await wrapper.Instance
            .PostAsync("/signup", body, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await DeserializeAuthResponse(response, cancellationToken)
            .ConfigureAwait(false);

        return new Jwt { Token = result.Token! };
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        throw new NotSupportedException();
    }

    public Task Unset(string key, CancellationToken _)
    {
        _config.RemoveParam(key);
        return Task.CompletedTask;
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(data);

        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        using var response = await wrapper.Instance
            .PutAsync($"/key/{data.Id.Table}/{data.Id.UnescapedId}", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);

        var okResult = EnsuresFirstResultOk(dbResponse);
        return okResult.DeserializeEnumerable<T>().First();
    }

    public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper(
            null,
            new UseConfiguration { Ns = ns, Db = db }
        );
        using var body = CreateBodyContent("RETURN TRUE");

        using var response = await wrapper.Instance
            .PostAsync("/sql", body, cancellationToken)
            .ConfigureAwait(false);

        var dbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
            .ConfigureAwait(false);
        EnsuresFirstResultOk(dbResponse);

        _config.Use(ns, db);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();

#if NET6_0_OR_GREATER
        return await wrapper.Instance
            .GetStringAsync("/version", cancellationToken)
            .ConfigureAwait(false);
#else
        return await wrapper.Instance.GetStringAsync("/version").ConfigureAwait(false);
#endif
    }

    private JsonSerializerOptions GetJsonSerializerOptions()
    {
        if (_configureJsonSerializerOptions is not null)
        {
            var jsonSerializerOptions = SurrealDbSerializerOptions.CreateJsonSerializerOptions();
            _configureJsonSerializerOptions(jsonSerializerOptions);

            return jsonSerializerOptions;
        }

        return SurrealDbSerializerOptions.Default;
    }

    private HttpClientWrapper CreateHttpClientWrapper(
        IAuth? overridedAuth = null,
        UseConfiguration? useConfiguration = null
    )
    {
        var client = CreateHttpClient(overridedAuth, useConfiguration);
        bool shouldDispose = !IsSingleHttpClient(client);

        return new HttpClientWrapper(client, shouldDispose);
    }

    private HttpClient CreateHttpClient(IAuth? overridedAuth, UseConfiguration? useConfiguration)
    {
        string? ns = useConfiguration is not null ? useConfiguration.Ns : _config.Ns;
        string? db = useConfiguration is not null ? useConfiguration.Db : _config.Db;

        var client = GetHttpClient();

        bool isSingleHttpClient = IsSingleHttpClient(client);

        if (isSingleHttpClient)
        {
            if (TrySetSingleHttpClientConfiguration(ns, db, _config.Auth))
            {
                ApplyHttpClientConfiguration(client, overridedAuth, useConfiguration);
                return client;
            }

            var desiredClientConfiguration = new HttpClientConfiguration(
                ns,
                db,
                overridedAuth ?? _config.Auth
            );
            bool shouldClone = _singleHttpClientConfiguration != desiredClientConfiguration;

            if (shouldClone)
            {
                var newHttpClient = new HttpClient();
                ApplyHttpClientConfiguration(newHttpClient, overridedAuth, useConfiguration);

                return newHttpClient;
            }
        }
        else
        {
            ApplyHttpClientConfiguration(client, overridedAuth, useConfiguration);
        }

        return client;
    }

    private void ApplyHttpClientConfiguration(
        HttpClient client,
        IAuth? overridedAuth,
        UseConfiguration? useConfiguration
    )
    {
        client.BaseAddress = _uri;

        client.DefaultRequestHeaders.Remove(HttpConstants.ACCEPT_HEADER_NAME);
        client.DefaultRequestHeaders.Remove(HttpConstants.NS_HEADER_NAME);
        client.DefaultRequestHeaders.Remove(HttpConstants.DB_HEADER_NAME);

        client.DefaultRequestHeaders.Add(
            HttpConstants.ACCEPT_HEADER_NAME,
            HttpConstants.ACCEPT_HEADER_VALUES
        );

        var ns = useConfiguration is not null ? useConfiguration.Ns : _config.Ns;
        var db = useConfiguration is not null ? useConfiguration.Db : _config.Db;

        client.DefaultRequestHeaders.Add(HttpConstants.NS_HEADER_NAME, ns);
        client.DefaultRequestHeaders.Add(HttpConstants.DB_HEADER_NAME, db);

        var auth = overridedAuth ?? _config.Auth;

        if (auth is BearerAuth bearerAuth)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                AuthConstants.BEARER,
                bearerAuth.Token
            );
        }
        if (auth is BasicAuth basicAuth)
        {
            string credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{basicAuth.Username}:{basicAuth.Password}")
            );
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                AuthConstants.BASIC,
                credentials
            );
        }
        if (auth is NoAuth)
        {
            client.DefaultRequestHeaders.Authorization = null;
        }
    }

    private HttpClient GetHttpClient()
    {
        if (_httpClientFactory is not null)
        {
            string httpClientName = HttpClientHelper.GetHttpClientName(_uri);
            return _httpClientFactory.CreateClient(httpClientName);
        }

        return _singleHttpClient.Value;
    }

    private readonly object _singleHttpClientConfigurationLock = new();

    private bool TrySetSingleHttpClientConfiguration(string? ns, string? db, IAuth auth)
    {
        lock (_singleHttpClientConfigurationLock)
        {
            if (_singleHttpClientConfiguration is null)
            {
                _singleHttpClientConfiguration = new HttpClientConfiguration(ns, db, auth);
                return true;
            }

            return false;
        }
    }

    private bool IsSingleHttpClient(HttpClient client)
    {
        return _singleHttpClient.IsValueCreated && client == _singleHttpClient.Value;
    }

    private StringContent CreateBodyContent<T>(T data)
    {
        string bodyContent = data is string str
            ? str
            : JsonSerializer.Serialize(data, GetJsonSerializerOptions());

        return new StringContent(bodyContent, Encoding.UTF8, "application/json");
    }

    private async Task<SurrealDbResponse> DeserializeDbResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
#if NET6_0_OR_GREATER
        using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var jsonSerializerOptions = GetJsonSerializerOptions();

        if (!response.IsSuccessStatusCode)
        {
            var result = await JsonSerializer
                .DeserializeAsync<ISurrealDbResult>(
                    stream,
                    jsonSerializerOptions,
                    cancellationToken
                )
                .ConfigureAwait(false);

            return new SurrealDbResponse(result!);
        }

        var list = new List<ISurrealDbResult>();

        await foreach (
            var result in JsonSerializer
                .DeserializeAsyncEnumerable<ISurrealDbResult>(
                    stream,
                    jsonSerializerOptions,
                    cancellationToken
                )
                .ConfigureAwait(false)
        )
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
        using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var authResponse = await JsonSerializer
            .DeserializeAsync<AuthResponse>(stream, GetJsonSerializerOptions(), cancellationToken)
            .ConfigureAwait(false);

        return authResponse is not null
            ? authResponse
            : throw new SurrealDbException("Cannot deserialize auth response");
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
}
