using System.Buffers;
using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dahomey.Cbor;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Internals;

#if NET8_0_OR_GREATER
[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
        | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(ISurrealDbHttpResponse))]
[JsonSerializable(typeof(SurrealDbHttpRequest))]
[JsonSerializable(typeof(ISurrealDbResult))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, object>))]
[JsonSerializable(typeof(RootAuth))]
[JsonSerializable(typeof(NamespaceAuth))]
[JsonSerializable(typeof(DatabaseAuth))]
[JsonSerializable(typeof(ScopeAuth))]
[JsonSerializable(typeof(AuthResponse))]
internal partial class SurrealDbHttpJsonSerializerContext : JsonSerializerContext;
#endif

internal class SurrealDbHttpEngine : ISurrealDbEngine
{
    private const string RPC_ENDPOINT = "/rpc";

    private readonly bool _useCbor;
    private readonly Uri _uri;
    private readonly SurrealDbClientParams _parameters;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly Action<JsonSerializerOptions>? _configureJsonSerializerOptions;
    private readonly Func<JsonSerializerContext[]>? _prependJsonSerializerContexts;
    private readonly Func<JsonSerializerContext[]>? _appendJsonSerializerContexts;
    private readonly Lazy<HttpClient> _singleHttpClient = new(() => new HttpClient(), true);
    private HttpClientConfiguration? _singleHttpClientConfiguration;
    private readonly SurrealDbHttpEngineConfig _config = new();

    public SurrealDbHttpEngine(
        SurrealDbClientParams parameters,
        IHttpClientFactory? httpClientFactory,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts
    )
    {
        _useCbor = parameters.Serialization?.ToLowerInvariant() == SerializationConstants.CBOR;
        _uri = new Uri(parameters.Endpoint!);
        _parameters = parameters;
        _httpClientFactory = httpClientFactory;
        _configureJsonSerializerOptions = configureJsonSerializerOptions;
        _prependJsonSerializerContexts = prependJsonSerializerContexts;
        _appendJsonSerializerContexts = appendJsonSerializerContexts;
    }

    public async Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest
        {
            Method = "authenticate",
            Parameters = [jwt.Token]
        };

        await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

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
        if (_useCbor)
        {
            string version = await Version(cancellationToken).ConfigureAwait(false);
            if (version.ToSemver().CompareSortOrderTo(new SemVersion(1, 4, 0)) < 0)
            {
                throw new SurrealDbException("CBOR is only supported on SurrealDB 1.4.0 or later.");
            }
        }

        var dbResponse = await RawQuery(
                "RETURN TRUE",
                ImmutableDictionary<string, object?>.Empty,
                cancellationToken
            )
            .ConfigureAwait(false);
        EnsuresFirstResultOk(dbResponse);
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        object?[] @params = _useCbor ? [data.Id, data] : [data.Id.ToString(), data];
        var request = new SurrealDbHttpRequest { Method = "create", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().First();
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [table] };

        await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        object?[] @params = _useCbor ? [thing] : [thing.ToString()];
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (dbResponse.Result.HasValue)
        {
            var valueKind = dbResponse.Result.Value.ValueKind;
            return valueKind != JsonValueKind.Null && valueKind != JsonValueKind.Undefined;
        }

        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public void Dispose()
    {
        if (_singleHttpClient.IsValueCreated)
            _singleHttpClient.Value.Dispose();
    }

    public async Task<bool> Health(CancellationToken cancellationToken)
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(new SurrealDbHttpRequest { Method = "ping" });

        try
        {
            using var response = await wrapper
                .Instance.PostAsync(RPC_ENDPOINT, body, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "info" };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.GetValue<T>()!;
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
        FormattableString query,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
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
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        object?[] @params = _useCbor ? [data.Id, data] : [data.Id.ToWsString(), data];
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Merge<T>(
        Thing thing,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        object?[] @params = _useCbor ? [thing, data] : [thing.ToWsString(), data];
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<TOutput>> MergeAll<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<IEnumerable<T>> MergeAll<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> Patch<T>(
        Thing thing,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        object?[] @params = _useCbor ? [thing, patches] : [thing.ToWsString(), patches];
        var request = new SurrealDbHttpRequest { Method = "patch", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> PatchAll<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var request = new SurrealDbHttpRequest { Method = "patch", Parameters = [table, patches] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<SurrealDbResponse> Query(
        FormattableString query,
        CancellationToken cancellationToken
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return await RawQuery(formattedQuery, parameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
    {
        var allParameters = new Dictionary<string, object?>(
            _config.Parameters.Count + parameters.Count
        );

        foreach (var (key, value) in _config.Parameters)
        {
            allParameters.Add(key, value);
        }

        foreach (var (key, value) in parameters)
        {
            allParameters.Add(key, value);
        }

        var request = new SurrealDbHttpRequest
        {
            Method = "query",
            Parameters = [query, allParameters]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var list = dbResponse.GetValue<List<ISurrealDbResult>>() ?? [];
        return new SurrealDbResponse(list);
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [table] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        object?[] @params = _useCbor ? [thing] : [thing.ToWsString()];
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task Set(string key, object value, CancellationToken cancellationToken)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (!key.IsValidVariableName())
        {
            throw new ArgumentException("Variable name is not valid.", nameof(key));
        }

        bool shouldEscapeKey = Thing.ShouldEscapeString(key);
        string escapedKey = shouldEscapeKey ? Thing.CreateEscaped(key) : key;

        var dbResponse = await RawQuery(
                $"RETURN ${escapedKey}",
                new Dictionary<string, object?>(capacity: 1) { { key, value } },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsuresFirstResultOk(dbResponse);

        _config.SetParam(key, value);
    }

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [rootAuth] };

        await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        _config.SetBasicAuth(rootAuth.Username, rootAuth.Password);
    }

    public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [nsAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [dbAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [scopeAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var request = new SurrealDbHttpRequest { Method = "signup", Parameters = [scopeAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt { Token = token! };
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        throw new NotSupportedException();
    }

    public Task Unset(string key, CancellationToken _)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (!key.IsValidVariableName())
        {
            throw new ArgumentException("Variable name is not valid.", nameof(key));
        }

        _config.RemoveParam(key);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<T>> UpdateAll<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        object?[] @params = _useCbor ? [data.Id, data] : [data.Id.ToWsString(), data];
        var request = new SurrealDbHttpRequest { Method = "update", Parameters = @params };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "use", Parameters = [ns, db] };
        await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        _config.Use(ns, db);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "version" };
        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.GetValue<string>()!;
    }

    private CurrentJsonSerializerOptionsForAot? _currentJsonSerializerOptionsForAot;

    private JsonSerializerOptions GetJsonSerializerOptions()
    {
        var jsonSerializerOptions = SurrealDbSerializerOptions.GetJsonSerializerOptions(
#if NET8_0_OR_GREATER
            SurrealDbHttpJsonSerializerContext.Default,
#endif
            _parameters.NamingPolicy,
            _configureJsonSerializerOptions,
            _prependJsonSerializerContexts,
            _appendJsonSerializerContexts,
            _currentJsonSerializerOptionsForAot,
            out var updatedJsonSerializerOptionsForAot
        );

        if (updatedJsonSerializerOptionsForAot is not null)
        {
            _currentJsonSerializerOptionsForAot = updatedJsonSerializerOptionsForAot;
        }

        return jsonSerializerOptions;
    }

    private CborOptions GetCborSerializerOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(_parameters.NamingPolicy);
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
            _useCbor ? ["application/cbor"] : ["application/json"]
        );

        var ns = useConfiguration is not null ? useConfiguration.Ns : _config.Ns;
        var db = useConfiguration is not null ? useConfiguration.Db : _config.Db;

        client.DefaultRequestHeaders.Add(HttpConstants.NS_HEADER_NAME, ns);
        client.DefaultRequestHeaders.Add(HttpConstants.DB_HEADER_NAME, db);

        var auth = overridedAuth ?? _config.Auth;

        switch (auth)
        {
            case BearerAuth bearerAuth:
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    AuthConstants.BEARER,
                    bearerAuth.Token
                );
                break;
            case BasicAuth basicAuth:
            {
                string credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{basicAuth.Username}:{basicAuth.Password}")
                );
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    AuthConstants.BASIC,
                    credentials
                );
                break;
            }
            case NoAuth:
                client.DefaultRequestHeaders.Authorization = null;
                break;
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

    private static StringContent CreateBodyContent(string data)
    {
        return new StringContentWithoutCharset(data, Encoding.UTF8, "application/json");
    }

    private HttpContent CreateBodyContent<T>(T data)
    {
        if (_useCbor)
        {
            var writer = new ArrayBufferWriter<byte>();
            CborSerializer.Serialize(data, writer, GetCborSerializerOptions());
            var payload = writer.WrittenSpan.ToArray();

            var content = new ByteArrayContent(payload);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");

            return content;
        }

        string bodyContent = JsonSerializer.IsReflectionEnabledByDefault
            ?
#pragma warning disable IL2026, IL3050
            JsonSerializer.Serialize(data, GetJsonSerializerOptions())
#pragma warning restore IL2026, IL3050
            : JsonSerializer.Serialize(
                data,
                (GetJsonSerializerOptions().GetTypeInfo(typeof(T)) as JsonTypeInfo<T>)!
            );

        return CreateBodyContent(bodyContent);
    }

    private async Task<SurrealDbHttpOkResponse> ExecuteRequestAsync(
        SurrealDbHttpRequest request,
        CancellationToken cancellationToken
    )
    {
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(request);

        using var response = await wrapper
            .Instance.PostAsync(RPC_ENDPOINT, body, cancellationToken)
            .ConfigureAwait(false);

        return await DeserializeDbResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SurrealDbHttpOkResponse> DeserializeDbResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
#if NET6_0_OR_GREATER
        using var stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        ISurrealDbHttpResponse? result;

        if (_useCbor)
        {
            var cborSerializerOptions = GetCborSerializerOptions();

            result = await CborSerializer
                .DeserializeAsync<ISurrealDbHttpResponse>(
                    stream,
                    cborSerializerOptions,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        else
        {
            var jsonSerializerOptions = GetJsonSerializerOptions();

#if NET8_0_OR_GREATER
            var taskResult = JsonSerializer.IsReflectionEnabledByDefault
                ?
#pragma warning disable IL2026, IL3050
                JsonSerializer.DeserializeAsync<ISurrealDbHttpResponse>(
                    stream,
                    jsonSerializerOptions,
                    cancellationToken
                )
#pragma warning restore IL2026, IL3050
                : JsonSerializer.DeserializeAsync(
                    stream,
                    (
                        jsonSerializerOptions.GetTypeInfo(typeof(ISurrealDbHttpResponse))
                        as JsonTypeInfo<ISurrealDbHttpResponse>
                    )!,
                    cancellationToken
                );

            result = await taskResult.ConfigureAwait(false);
#else
            result = await JsonSerializer
                .DeserializeAsync<ISurrealDbHttpResponse>(
                    stream,
                    jsonSerializerOptions,
                    cancellationToken
                )
                .ConfigureAwait(false);
#endif
        }

        return ExtractSurrealDbOkResponse(result);
    }

    private static SurrealDbHttpOkResponse ExtractSurrealDbOkResponse(
        ISurrealDbHttpResponse? result
    )
    {
        return result switch
        {
            SurrealDbHttpOkResponse okResponse => okResponse,
            SurrealDbHttpErrorResponse errorResponse
                => throw new SurrealDbException(errorResponse.Error.Message),
            _ => throw new SurrealDbException("Unknown response type"),
        };
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
