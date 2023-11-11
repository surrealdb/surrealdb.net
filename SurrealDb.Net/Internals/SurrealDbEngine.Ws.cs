using Microsoft.IO;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Json;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using Websocket.Client;

namespace SurrealDb.Net.Internals;

internal class SurrealDbWsEngine : ISurrealDbEngine
{
    private static readonly ConcurrentDictionary<string, SurrealDbWsEngine> _wsEngines = new();
    private static readonly ConcurrentDictionary<
        string,
        SurrealWsTaskCompletionSource
    > _responseTasks = new();
    private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();

    private readonly string _id;
    private readonly Uri _uri;
    private readonly Action<JsonSerializerOptions>? _configureJsonSerializerOptions;
    private readonly SurrealDbWsEngineConfig _config = new();
    private readonly WebsocketClient _wsClient;
    private readonly IDisposable _receiverSubscription;
    private readonly ConcurrentDictionary<
        Guid,
        SurrealDbLiveQueryChannelSubscriptions
    > _liveQueryChannelSubscriptionsPerQuery = new();

    public SurrealDbWsEngine(Uri uri, Action<JsonSerializerOptions>? configureJsonSerializerOptions)
    {
        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_wsEngines.TryAdd(id, this));

        _id = id;
        _uri = uri;
        _configureJsonSerializerOptions = configureJsonSerializerOptions;
        _wsClient = new WebsocketClient(_uri) { IsTextMessageConversionEnabled = false };

        _receiverSubscription = _wsClient.MessageReceived
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(
                message =>
                    Observable.FromAsync(
                        async (cancellationToken) =>
                        {
                            ISurrealDbWsResponse? response = null;

                            switch (message.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    response = JsonSerializer.Deserialize<ISurrealDbWsResponse>(
                                        message.Text!,
                                        GetJsonSerializerOptions()
                                    );
                                    break;
                                case WebSocketMessageType.Binary:
                                {
                                    using var stream = _memoryStreamManager.GetStream(
                                        message.Binary
                                    );

                                    response = await JsonSerializer
                                        .DeserializeAsync<ISurrealDbWsResponse>(
                                            stream,
                                            GetJsonSerializerOptions(),
                                            cancellationToken
                                        )
                                        .ConfigureAwait(false);
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
                                    var tasks =
                                        _liveQueryChannelSubscriptions.Select(liveQueryChannel =>
                                        {
                                            return liveQueryChannel.WriteAsync(
                                                surrealDbWsLiveResponse
                                            );
                                        });

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
        await SendRequest("authenticate", new() { jwt.Token }, cancellationToken)
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

        await _wsClient.StartOrFail().ConfigureAwait(false);

        if (_config.Ns is not null)
            await Use(_config.Ns, _config.Db!, cancellationToken).ConfigureAwait(false);

        if (_config.Auth is BasicAuth basicAuth)
            await SignIn(
                    new RootAuth { Username = basicAuth.Username, Password = basicAuth.Password! },
                    cancellationToken
                )
                .ConfigureAwait(false);

        if (_config.Auth is BearerAuth bearerAuth)
            await Authenticate(new Jwt { Token = bearerAuth.Token }, cancellationToken)
                .ConfigureAwait(false);

        _config.Reset();
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var dbResponse = await SendRequest(
                "create",
                new() { data.Id.ToString(), data },
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("create", new() { table, data }, cancellationToken)
            .ConfigureAwait(false);

        var list = dbResponse.GetValue<List<T>>() ?? new();
        return list.First();
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        await SendRequest("delete", new() { table }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("delete", new() { thing.ToString() }, cancellationToken)
            .ConfigureAwait(false);

        var valueKind = dbResponse.Result.ValueKind;
        return valueKind != JsonValueKind.Null && valueKind != JsonValueKind.Undefined;
    }

    public void Dispose()
    {
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
                responseTaskCompletionSource.SetCanceled();
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
            await _wsClient.StartOrFail().ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<T> Info<T>(CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("info", null, cancellationToken).ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task Invalidate(CancellationToken cancellationToken)
    {
        await SendRequest("invalidate", null, cancellationToken).ConfigureAwait(false);
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

        await SendRequest("kill", new() { queryUuid.ToString() }, cancellationToken)
            .ConfigureAwait(false);
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid)
    {
        _liveQueryChannelSubscriptionsPerQuery.TryAdd(queryUuid, new(_id));
        return new SurrealDbLiveQuery<T>(queryUuid, this);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        string query,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await Query(query, parameters, cancellationToken).ConfigureAwait(false);

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
        var dbResponse = await SendRequest("live", new() { table, diff }, cancellationToken)
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

        var dbResponse = await SendRequest(
                "merge",
                new() { data.Id.ToWsString(), data },
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Merge<T>(
        Thing thing,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequest(
                "merge",
                new() { thing.ToWsString(), data },
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<SurrealDbResponse> Query(
        string query,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequest("query", new() { query, parameters }, cancellationToken)
            .ConfigureAwait(false);

        var list = dbResponse.GetValue<List<ISurrealDbResult>>() ?? new();
        return new SurrealDbResponse(list);
    }

    public async Task<List<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("select", new() { table }, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<List<T>>()!;
    }

    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest(
                "select",
                new() { thing.ToWsString() },
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task Set(string key, object value, CancellationToken cancellationToken)
    {
        await SendRequest("let", new() { key, value }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
    {
        await SendRequest("signin", new() { rootAuth }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("signin", new() { nsAuth }, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>()!;

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("signin", new() { dbAuth }, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>()!;

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var dbResponse = await SendRequest("signin", new() { scopeAuth }, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>()!;

        return new Jwt { Token = token! };
    }

    public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var dbResponse = await SendRequest("signup", new() { scopeAuth }, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>()!;

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
        await SendRequest("unset", new() { key }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : Record
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var dbResponse = await SendRequest(
                "update",
                new() { data.Id.ToWsString(), data },
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        await SendRequest("use", new() { ns, db }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequest("version", null, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<string>()!;
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

    private async Task<SurrealDbWsOkResponse> SendRequest(
        string method,
        List<object?>? parameters,
        CancellationToken cancellationToken
    )
    {
        if (!_wsClient.IsStarted)
        {
            await Connect(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        var taskCompletionSource = new SurrealWsTaskCompletionSource(_id);

        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_responseTasks.TryAdd(id, taskCompletionSource));

        bool shouldSendParamsInRequest = parameters is not null && parameters.Count > 0;

        var request = new SurrealDbWsRequest
        {
            Id = id,
            Method = method,
            Parameters = shouldSendParamsInRequest ? parameters : null,
        };

        using var stream = _memoryStreamManager.GetStream();

        await JsonSerializer
            .SerializeAsync(stream, request, GetJsonSerializerOptions(), cancellationToken)
            .ConfigureAwait(false);

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
#if NET7_0_OR_GREATER
        string payload = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
        string payload = await reader.ReadToEndAsync().ConfigureAwait(false);
#endif

        _wsClient.Send(payload);

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
