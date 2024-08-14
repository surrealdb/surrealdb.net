﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Cbor;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Stream;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;
using Websocket.Client;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace SurrealDb.Net.Internals;

#if NET8_0_OR_GREATER
[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
        | JsonNumberHandling.AllowNamedFloatingPointLiterals,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(ISurrealDbWsResponse))]
[JsonSerializable(typeof(SurrealDbWsRequest))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, object>))]
[JsonSerializable(typeof(List<ISurrealDbResult>))]
[JsonSerializable(typeof(RootAuth))]
[JsonSerializable(typeof(NamespaceAuth))]
[JsonSerializable(typeof(DatabaseAuth))]
[JsonSerializable(typeof(ScopeAuth))]
internal partial class SurrealDbWsJsonSerializerContext : JsonSerializerContext;
#endif

internal class SurrealDbWsEngine : ISurrealDbEngine
{
    private static readonly ConcurrentDictionary<string, SurrealDbWsEngine> _wsEngines = new();
    private static readonly ConcurrentDictionary<
        string,
        SurrealWsTaskCompletionSource
    > _responseTasks = new();

    private readonly bool _useCbor;
    private readonly string _id;
    private readonly SurrealDbClientParams _parameters;
    private readonly Action<JsonSerializerOptions>? _configureJsonSerializerOptions;
    private readonly Func<JsonSerializerContext[]>? _prependJsonSerializerContexts;
    private readonly Func<JsonSerializerContext[]>? _appendJsonSerializerContexts;
    private readonly Action<CborOptions>? _configureCborOptions;
    private readonly SurrealDbWsEngineConfig _config = new();
    private readonly WebsocketClient _wsClient;
    private readonly IDisposable _receiverSubscription;
    private readonly ConcurrentDictionary<
        Guid,
        SurrealDbLiveQueryChannelSubscriptions
    > _liveQueryChannelSubscriptionsPerQuery = new();
    private readonly Pinger _pinger;

    private bool _isInitialized;

    public SurrealDbWsEngine(
        SurrealDbClientParams parameters,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_wsEngines.TryAdd(id, this));

        _useCbor = SurrealDbEngineHelpers.ShouldUseCbor(parameters);
        _id = id;
        _parameters = parameters;
        _configureJsonSerializerOptions = configureJsonSerializerOptions;
        _prependJsonSerializerContexts = prependJsonSerializerContexts;
        _appendJsonSerializerContexts = appendJsonSerializerContexts;
        _configureCborOptions = configureCborOptions;

        var clientWebSocketFactory = _useCbor
            ? new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket();
                client.Options.AddSubProtocol(SerializationConstants.CBOR);

                return client;
            })
            : null;

        _wsClient = new WebsocketClient(new Uri(parameters.Endpoint!), clientWebSocketFactory)
        {
            IsTextMessageConversionEnabled = false,
            IsStreamDisposedAutomatically = false
        };
        _pinger = new(Ping);

        _receiverSubscription = _wsClient
            .MessageReceived.ObserveOn(TaskPoolScheduler.Default)
            .Select(message =>
                Observable.FromAsync(
                    async (cancellationToken) =>
                    {
                        ISurrealDbWsResponse? response = null;

                        switch (message.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                if (!_useCbor)
                                {
#if NET8_0_OR_GREATER
                                    if (JsonSerializer.IsReflectionEnabledByDefault)
                                    {
#pragma warning disable IL2026, IL3050
                                        response = JsonSerializer.Deserialize<ISurrealDbWsResponse>(
                                            message.Text!,
                                            GetJsonSerializerOptions()
                                        );
#pragma warning restore IL2026, IL3050
                                    }
                                    else
                                    {
                                        response = JsonSerializer.Deserialize(
                                            message.Text!,
                                            (
                                                GetJsonSerializerOptions()
                                                    .GetTypeInfo(typeof(ISurrealDbWsResponse))
                                                as JsonTypeInfo<ISurrealDbWsResponse>
                                            )!
                                        );
                                    }
#else
                                    response = JsonSerializer.Deserialize<ISurrealDbWsResponse>(
                                        message.Text!,
                                        GetJsonSerializerOptions()
                                    );
#endif
                                }
                                break;
                            case WebSocketMessageType.Binary:
                            {
                                using var stream = message.Stream is not null
                                    ? message.Stream
                                    : MemoryStreamProvider.MemoryStreamManager.GetStream(
                                        message.Binary!
                                    );

                                if (_useCbor)
                                {
                                    response = await CborSerializer
                                        .DeserializeAsync<ISurrealDbWsResponse>(
                                            stream,
                                            GetCborOptions(),
                                            cancellationToken
                                        )
                                        .ConfigureAwait(false);
                                }
                                else
                                {
#if NET8_0_OR_GREATER
                                    if (JsonSerializer.IsReflectionEnabledByDefault)
                                    {
#pragma warning disable IL2026, IL3050
                                        response = await JsonSerializer
                                            .DeserializeAsync<ISurrealDbWsResponse>(
                                                stream,
                                                GetJsonSerializerOptions(),
                                                cancellationToken
                                            )
                                            .ConfigureAwait(false);
#pragma warning restore IL2026, IL3050
                                    }
                                    else
                                    {
                                        response = await JsonSerializer
                                            .DeserializeAsync(
                                                stream,
                                                (
                                                    GetJsonSerializerOptions()
                                                        .GetTypeInfo(typeof(ISurrealDbWsResponse))
                                                    as JsonTypeInfo<ISurrealDbWsResponse>
                                                )!,
                                                cancellationToken
                                            )
                                            .ConfigureAwait(false);
                                    }
#else
                                    response = await JsonSerializer
                                        .DeserializeAsync<ISurrealDbWsResponse>(
                                            stream,
                                            GetJsonSerializerOptions(),
                                            cancellationToken
                                        )
                                        .ConfigureAwait(false);
#endif
                                }

                                break;
                            }
                        }

                        if (response is SurrealDbWsLiveResponse surrealDbWsLiveResponse)
                        {
                            var liveQueryUuid = surrealDbWsLiveResponse.Result.Id;

                            if (
                                _liveQueryChannelSubscriptionsPerQuery.TryGetValue(
                                    liveQueryUuid,
                                    out var _liveQueryChannelSubscriptions
                                )
                            )
                            {
                                var tasks = _liveQueryChannelSubscriptions.Select(
                                    liveQueryChannel =>
                                    {
                                        return liveQueryChannel.WriteAsync(surrealDbWsLiveResponse);
                                    }
                                );

                                await Task.WhenAll(tasks).ConfigureAwait(false);
                            }

                            return;
                        }

                        if (
                            response is ISurrealDbWsStandardResponse surrealDbWsStandardResponse
                            && _responseTasks.TryRemove(
                                surrealDbWsStandardResponse.Id,
                                out var responseTaskCompletionSource
                            )
                        )
                        {
                            switch (response)
                            {
                                case SurrealDbWsOkResponse okResponse:
                                    responseTaskCompletionSource.SetResult(okResponse);
                                    break;
                                case SurrealDbWsErrorResponse errorResponse:
                                    responseTaskCompletionSource.SetException(
                                        new SurrealDbException(errorResponse.Error.Message)
                                    );
                                    break;
                                default:
                                    responseTaskCompletionSource.SetException(
                                        new SurrealDbException("Unknown response type")
                                    );
                                    break;
                            }
                        }
                    }
                )
            )
            .Merge()
            .Subscribe();
    }

    public async Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        await SendRequestAsync("authenticate", [jwt.Token], false, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Configure(string? ns, string? db, string? username, string? password)
    {
        // 💡 Pre-configuration before connect
        if (ns is not null)
            _config.Use(ns, db);

        if (username is not null)
            _config.SetBasicAuth(username, password);
    }

    public void Configure(string? ns, string? db, string? token = null)
    {
        // 💡 Pre-configuration before connect
        if (ns is not null)
            _config.Use(ns, db);

        if (token is not null)
            _config.SetBearerAuth(token);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
            throw new SurrealDbException("Client already started");

        _isInitialized = false;

        await _wsClient.StartOrFail().ConfigureAwait(false);

        if (_config.Ns is not null)
        {
            await Use(_config.Ns, _config.Db!, cancellationToken).ConfigureAwait(false);
        }

        if (_config.Auth is BasicAuth basicAuth)
        {
            await SignIn(
                    new RootAuth { Username = basicAuth.Username, Password = basicAuth.Password! },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        if (_config.Auth is BearerAuth bearerAuth)
        {
            await Authenticate(new Jwt { Token = bearerAuth.Token }, cancellationToken)
                .ConfigureAwait(false);
        }

        if (_useCbor)
        {
            string version = await Version(cancellationToken).ConfigureAwait(false);
            if (version.ToSemver().CompareSortOrderTo(new SemVersion(1, 4, 0)) < 0)
            {
                throw new SurrealDbException("CBOR is only supported on SurrealDB 1.4.0 or later.");
            }
        }

        _pinger.Start();
        _isInitialized = true;
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        object?[] @params = _useCbor ? [data.Id, data] : [data.Id.ToString(), data];

        var dbResponse = await SendRequestAsync("create", @params, true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync("create", [table, data], true, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().First();
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        await SendRequestAsync("delete", [table], true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        object?[] @params = _useCbor ? [thing] : [thing.ToString()];

        var dbResponse = await SendRequestAsync("delete", @params, true, cancellationToken)
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
        _pinger.Dispose();

        var endChannelsTasks = new List<Task>();

        foreach (var (key, value) in _liveQueryChannelSubscriptionsPerQuery)
        {
            if (
                value.WsEngineId == _id
                && _liveQueryChannelSubscriptionsPerQuery.TryRemove(
                    key,
                    out var _liveQueryChannelSubscriptions
                )
            )
            {
                foreach (var liveQueryChannel in _liveQueryChannelSubscriptions)
                {
                    var closeTask = CloseLiveQueryAsync(
                        liveQueryChannel,
                        SurrealDbLiveQueryClosureReason.SocketClosed
                    );
                    endChannelsTasks.Add(closeTask);

                    var killTask = Kill(key, SurrealDbLiveQueryClosureReason.SocketClosed, default);
                    endChannelsTasks.Add(killTask);
                }
            }
        }

        if (endChannelsTasks.Any())
        {
            try
            {
                Task.WhenAll(endChannelsTasks).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (SurrealDbException) { }
            catch (OperationCanceledException) { }
        }

        _wsClient.Stop(WebSocketCloseStatus.NormalClosure, "Client disposed");
        _receiverSubscription.Dispose();

        foreach (var (key, value) in _responseTasks)
        {
            if (
                value.WsEngineId == _id
                && _responseTasks.TryRemove(key, out var responseTaskCompletionSource)
            )
            {
                responseTaskCompletionSource.TrySetCanceled();
            }
        }

        _wsEngines.TryRemove(_id, out _);

        _wsClient.Dispose();
    }

    public async Task<bool> Health(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
            return true;

        try
        {
            await InternalConnectAsync(true, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<T> Info<T>(CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync("info", null, true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task Invalidate(CancellationToken cancellationToken)
    {
        await SendRequestAsync("invalidate", null, false, cancellationToken).ConfigureAwait(false);
    }

    public async Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        CancellationToken cancellationToken
    )
    {
        if (
            _liveQueryChannelSubscriptionsPerQuery.TryRemove(
                queryUuid,
                out var _liveQueryChannelSubscriptions
            )
        )
        {
            var tasks = _liveQueryChannelSubscriptions.Select(liveQueryChannel =>
            {
                return CloseLiveQueryAsync(liveQueryChannel, reason);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        object?[] @params = _useCbor ? [queryUuid] : [queryUuid.ToString()];

        await SendRequestAsync("kill", @params, true, cancellationToken).ConfigureAwait(false);
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid)
    {
        _liveQueryChannelSubscriptionsPerQuery.TryAdd(queryUuid, new(_id));
        return new SurrealDbLiveQuery<T>(queryUuid, this);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        FormattableString query,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await Query(query, cancellationToken).ConfigureAwait(false);

        if (dbResponse.HasErrors)
        {
            throw new SurrealDbErrorResultException(dbResponse.FirstError!);
        }

        if (dbResponse.FirstOk is null)
        {
            throw new SurrealDbErrorResultException();
        }

        var queryUuid = dbResponse.FirstOk.GetValue<Guid>()!;

        return ListenLive<T>(queryUuid);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await RawQuery(query, parameters, cancellationToken).ConfigureAwait(false);

        if (dbResponse.HasErrors)
        {
            throw new SurrealDbErrorResultException(dbResponse.FirstError!);
        }

        if (dbResponse.FirstOk is null)
        {
            throw new SurrealDbErrorResultException();
        }

        var queryUuid = dbResponse.FirstOk.GetValue<Guid>()!;

        return ListenLive<T>(queryUuid);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync("live", [table, diff], true, cancellationToken)
            .ConfigureAwait(false);
        var queryUuid = dbResponse.GetValue<Guid>()!;

        return ListenLive<T>(queryUuid);
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

        var dbResponse = await SendRequestAsync("merge", @params, true, cancellationToken)
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

        var dbResponse = await SendRequestAsync("merge", @params, true, cancellationToken)
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
        var dbResponse = await SendRequestAsync("merge", [table, data], true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<IEnumerable<T>> MergeAll<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync("merge", [table, data], true, cancellationToken)
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

        var dbResponse = await SendRequestAsync("patch", @params, true, cancellationToken)
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
        var dbResponse = await SendRequestAsync("patch", [table, patches], true, cancellationToken)
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
        var dbResponse = await SendRequestAsync(
                "query",
                [query, parameters],
                true,
                cancellationToken
            )
            .ConfigureAwait(false);

        var list = dbResponse.GetValue<List<ISurrealDbResult>>() ?? [];
        return new SurrealDbResponse(list);
    }

    public async Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<Thing> ins,
        IEnumerable<Thing> outs,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [ins, table, outs, data],
                true,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<TOutput> Relate<TOutput, TData>(
        Thing thing,
        Thing @in,
        Thing @out,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [@in, thing, @out, data],
                true,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync("select", [table], true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>()!;
    }

    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        object?[] @params = _useCbor ? [thing] : [thing.ToWsString()];

        var dbResponse = await SendRequestAsync("select", @params, true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken)
    {
        if (!_useCbor)
        {
            throw new NotImplementedException(
                $"Selecting by {nameof(StringRecordId)} is only available via CBOR serialization."
            );
        }

        var dbResponse = await SendRequestAsync("select", [recordId], true, cancellationToken)
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

        await SendRequestAsync("let", [key, value], false, cancellationToken).ConfigureAwait(false);
    }

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
    {
        await SendRequestAsync("signin", [rootAuth], false, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync("signin", [nsAuth], false, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync("signin", [dbAuth], false, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync("signin", [scopeAuth], false, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync("signup", [scopeAuth], false, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        return new Jwt { Token = token! };
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        if (
            !_liveQueryChannelSubscriptionsPerQuery.TryGetValue(
                id,
                out var _liveQueryChannelSubscriptions
            )
        )
        {
            throw new SurrealDbException("Live Query not found");
        }

        var liveQueryChannel = new SurrealDbLiveQueryChannel();
        _liveQueryChannelSubscriptions.Add(liveQueryChannel);

        return liveQueryChannel;
    }

    public async Task Unset(string key, CancellationToken cancellationToken)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (!key.IsValidVariableName())
        {
            throw new ArgumentException("Variable name is not valid.", nameof(key));
        }

        await SendRequestAsync("unset", [key], false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> UpdateAll<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync("update", [table, data], true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        object?[] @params = _useCbor ? [data.Id, data] : [data.Id.ToWsString(), data];

        var dbResponse = await SendRequestAsync("update", @params, true, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        await SendRequestAsync("use", [ns, db], false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync("version", null, false, cancellationToken)
            .ConfigureAwait(false);
        string version = dbResponse.GetValue<string>()!;

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private CurrentJsonSerializerOptionsForAot? _currentJsonSerializerOptionsForAot;

    private JsonSerializerOptions GetJsonSerializerOptions()
    {
        var jsonSerializerOptions = SurrealDbSerializerOptions.GetJsonSerializerOptions(
#if NET8_0_OR_GREATER
            SurrealDbWsJsonSerializerContext.Default,
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

    private CborOptions GetCborOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(
            _parameters.NamingPolicy,
            _configureCborOptions
        );
    }

    private async Task Ping(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
        {
            await SendRequestAsync("ping", null, false, cancellationToken).ConfigureAwait(false);
        }
    }

    private readonly SemaphoreSlim _semaphoreConnect = new(1, 1);

    /// <summary>
    /// Avoid multiple connections in a multi-threading context
    /// and prevent usage before initialized
    /// </summary>
    private async Task InternalConnectAsync(
        bool requireInitialized,
        CancellationToken cancellationToken
    )
    {
        if (!_wsClient.IsStarted || (requireInitialized && !_isInitialized))
        {
            await _semaphoreConnect.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_wsClient.IsStarted)
                {
                    await Connect(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphoreConnect.Release();
            }
        }
    }

    private async Task<SurrealDbWsOkResponse> SendRequestAsync(
        string method,
        object?[]? parameters,
        bool requireInitialized,
        CancellationToken cancellationToken
    )
    {
        await InternalConnectAsync(requireInitialized, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var taskCompletionSource = new SurrealWsTaskCompletionSource(_id);

        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_responseTasks.TryAdd(id, taskCompletionSource));

        bool shouldSendParamsInRequest = parameters is not null && parameters.Length > 0;

        var request = new SurrealDbWsRequest
        {
            Id = id,
            Method = method,
            Parameters = shouldSendParamsInRequest ? parameters : null,
        };

        await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();
        bool isMessageSent;

        if (_useCbor)
        {
            await CborSerializer
                .SerializeAsync(request, stream, GetCborOptions(), cancellationToken)
                .ConfigureAwait(false);

            bool canGetBuffer = stream.TryGetBuffer(out var payload);
            isMessageSent = canGetBuffer && _wsClient.Send(payload);
        }
        else
        {
#if NET8_0_OR_GREATER
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
#pragma warning disable IL2026, IL3050
                await JsonSerializer
                    .SerializeAsync(stream, request, GetJsonSerializerOptions(), cancellationToken)
                    .ConfigureAwait(false);
#pragma warning restore IL2026, IL3050
            }
            else
            {
                await JsonSerializer
                    .SerializeAsync(
                        stream,
                        request,
                        GetJsonSerializerOptions().GetTypeInfo(typeof(SurrealDbWsRequest)),
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
#else
            await JsonSerializer
                .SerializeAsync(stream, request, GetJsonSerializerOptions(), cancellationToken)
                .ConfigureAwait(false);
#endif

            bool canGetBuffer = stream.TryGetBuffer(out var payload);
            isMessageSent = canGetBuffer && _wsClient.SendAsText(payload);
        }

        if (!isMessageSent)
        {
            taskCompletionSource.SetException(new SurrealDbException("Failed to send message"));
            throw new SurrealDbException("Failed to send message");
        }

        var response = await taskCompletionSource.Task.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return response;
    }

    private static async Task CloseLiveQueryAsync(
        SurrealDbLiveQueryChannel liveQueryChannel,
        SurrealDbLiveQueryClosureReason reason
    )
    {
        await liveQueryChannel
            .WriteAsync(new SurrealDbWsClosedLiveResponse { Reason = reason })
            .ConfigureAwait(false);

        liveQueryChannel.Complete();
    }
}
