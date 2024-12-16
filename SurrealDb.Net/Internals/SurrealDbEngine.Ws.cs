using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ConcurrentCollections;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using Serilog;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Stream;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;
using Websocket.Client;

namespace SurrealDb.Net.Internals;

internal class SurrealDbWsEngine : ISurrealDbEngine
{
    private static readonly ConcurrentDictionary<string, SurrealDbWsEngine> _wsEngines = new();

    internal SemVersion? _version { get; private set; }
    internal Action<CborOptions>? _configureCborOptions { get; }
    internal SurrealDbWsEngineConfig _config { get; }

    private readonly string _id;
    private readonly SurrealDbOptions _parameters;
    private readonly ISurrealDbLoggerFactory? _surrealDbLoggerFactory;
    private readonly WebsocketClient _wsClient;
    private readonly IDisposable _receiverSubscription;
    private readonly ConcurrentDictionary<
        Guid,
        SurrealDbLiveQueryChannelSubscriptions
    > _liveQueryChannelSubscriptionsPerQuery = new();
    private readonly Pinger _pinger;
    private readonly WsResponseTaskHandler _responseTaskHandler;
    private readonly SemaphoreSlim _semaphoreConnect = new(1, 1);

    private bool _isInitialized;

#if DEBUG
    public string Id => _id;
#endif

    public SurrealDbWsEngine(
        SurrealDbOptions parameters,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory
    )
    {
        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_wsEngines.TryAdd(id, this));

        _id = id;
        _parameters = parameters;
        _configureCborOptions = configureCborOptions;
        _surrealDbLoggerFactory = surrealDbLoggerFactory;
        _config = new(_parameters);

        var clientWebSocketFactory = new Func<ClientWebSocket>(() =>
        {
            var client = new ClientWebSocket();
            client.Options.AddSubProtocol(SerializationConstants.CBOR);

            return client;
        });

        _wsClient = new WebsocketClient(new Uri(parameters.Endpoint!), clientWebSocketFactory)
        {
            IsTextMessageConversionEnabled = false,
            IsStreamDisposedAutomatically = false,
            ErrorReconnectTimeout = TimeSpan.FromSeconds(10),
        };
        _pinger = new(Ping);
        _responseTaskHandler = new(id);

        _receiverSubscription = _wsClient
            .MessageReceived.ObserveOn(TaskPoolScheduler.Default)
            .Select(message =>
                Observable.FromAsync(
                    async (cancellationToken) =>
                    {
                        ISurrealDbWsResponse? response = null;

                        if (message.MessageType == WebSocketMessageType.Binary)
                        {
                            using var stream =
                                message.Stream
                                ?? MemoryStreamProvider.MemoryStreamManager.GetStream(
                                    message.Binary!
                                );

                            response = await CborSerializer
                                .DeserializeAsync<ISurrealDbWsResponse>(
                                    stream,
                                    GetCborOptions(),
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }

                        if (response is SurrealDbWsLiveResponse surrealDbWsLiveResponse)
                        {
                            var liveQueryUuid = surrealDbWsLiveResponse.Result.Id;

                            if (
                                _liveQueryChannelSubscriptionsPerQuery.TryGetValue(
                                    liveQueryUuid,
                                    out var liveQueryChannelSubscriptions
                                )
                            )
                            {
                                var tasks = liveQueryChannelSubscriptions.Select(liveQueryChannel =>
                                {
                                    return liveQueryChannel.WriteAsync(surrealDbWsLiveResponse);
                                });

                                await Task.WhenAll(tasks).ConfigureAwait(false);
                            }

                            return;
                        }

                        if (
                            response is ISurrealDbWsStandardResponse surrealDbWsStandardResponse
                            && _responseTaskHandler.TryRemove(
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

        _wsClient
            .ReconnectionHappened.Where(info => info.Type != ReconnectionType.Initial)
            .Select(_ =>
                Observable.FromAsync(
                    async (cancellationToken) =>
                    {
                        await ApplyConfigurationAsync(cancellationToken).ConfigureAwait(false);
                    }
                )
            )
            .Switch()
            .Subscribe();

        _wsClient.DisconnectionHappened.Subscribe(
            async (_) =>
            {
                var endChannelsTasks = new List<Task>();

                foreach (var (key, value) in _liveQueryChannelSubscriptionsPerQuery)
                {
                    if (
                        value.WsEngineId == _id
                        && _liveQueryChannelSubscriptionsPerQuery.TryRemove(
                            key,
                            out var liveQueryChannelSubscriptions
                        )
                    )
                    {
                        foreach (var liveQueryChannel in liveQueryChannelSubscriptions)
                        {
                            var closeTask = CloseLiveQueryAsync(
                                liveQueryChannel,
                                SurrealDbLiveQueryClosureReason.SocketClosed
                            );
                            endChannelsTasks.Add(closeTask);
                        }
                    }
                }

                if (endChannelsTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(endChannelsTasks).ConfigureAwait(false);
                    }
                    catch { }
                }
            }
        );
    }

    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        return Authenticate(jwt, SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task Authenticate(
        Jwt jwt,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync("authenticate", [jwt.Token], priority, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Clear(CancellationToken cancellationToken)
    {
        await SendRequestAsync("clear", null, SurrealDbWsRequestPriority.Normal, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
            throw new SurrealDbException("Client already started");

        _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters.Endpoint!);

        _isInitialized = false;

        await _wsClient.StartOrFail().ConfigureAwait(false);

        await ApplyConfigurationAsync(cancellationToken).ConfigureAwait(false);

        string version = await Version(SurrealDbWsRequestPriority.High, cancellationToken)
            .ConfigureAwait(false);
        _version = version.ToSemver();

        if (_version.CompareSortOrderTo(new SemVersion(1, 4, 0)) < 0)
        {
            throw new SurrealDbException("CBOR is only supported on SurrealDB 1.4.0 or later.");
        }

        _pinger.Start();
        _isInitialized = true;

        _surrealDbLoggerFactory?.Connection?.LogConnectionSuccess(_parameters.Endpoint!);
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var dbResponse = await SendRequestAsync(
                "create",
                [data.Id, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "create",
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (_version?.Major > 1)
        {
            return dbResponse.GetValue<T>()!;
        }
        return dbResponse.DeserializeEnumerable<T>().First();
    }

    public async Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "create",
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        await SendRequestAsync(
                "delete",
                [table],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "delete",
                [recordId],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public async Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "delete",
                [recordId],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisposeAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _pinger.Dispose();

        int expectedEndChannelsTasksCount =
            _liveQueryChannelSubscriptionsPerQuery.Sum(kv => kv.Value.Count) * 2;

        var endChannelsTasks = new List<Task>(expectedEndChannelsTasksCount);

        foreach (var (key, value) in _liveQueryChannelSubscriptionsPerQuery)
        {
            if (
                value.WsEngineId == _id
                && _liveQueryChannelSubscriptionsPerQuery.TryRemove(
                    key,
                    out var liveQueryChannelSubscriptions
                )
            )
            {
                foreach (var liveQueryChannel in liveQueryChannelSubscriptions)
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

        if (endChannelsTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(endChannelsTasks).ConfigureAwait(false);
            }
            catch (SurrealDbException) { }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
        }

        await _wsClient.Stop(WebSocketCloseStatus.NormalClosure, "Client disposed");
        _receiverSubscription.Dispose();

        foreach (var (key, value) in _responseTaskHandler)
        {
            _responseTaskHandler.TryRemove(key, out _);
            value.TrySetCanceled();
        }

        _wsEngines.TryRemove(_id, out _);

        _wsClient.Dispose();
        _semaphoreConnect.Dispose();

        _disposed = true;
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
        var dbResponse = await SendRequestAsync(
                "info",
                null,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "insert",
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken)
        where T : RelationRecord
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbException("Cannot create a relation record without an Id");

        var dbResponse = await SendRequestAsync(
                "insert_relation",
                [null, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
    }

    public async Task<T> InsertRelation<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : RelationRecord
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        if (data.Id is not null)
            throw new SurrealDbException(
                "You cannot provide both the table and an Id for the record. Either use the method overload without 'table' param or set the Id property to null."
            );

        var dbResponse = await SendRequestAsync(
                "insert_relation",
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
    }

    public async Task Invalidate(CancellationToken cancellationToken)
    {
        await SendRequestAsync(
                "invalidate",
                null,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        _config.ResetAuth();
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

        await SendRequestAsync(
                "kill",
                [queryUuid],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid)
    {
        _liveQueryChannelSubscriptionsPerQuery.TryAdd(queryUuid, new(_id));
        return new SurrealDbLiveQuery<T>(queryUuid, this);
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
        var dbResponse = await SendRequestAsync(
                "live",
                [table, diff],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var queryUuid = dbResponse.GetValue<Guid>()!;

        return ListenLive<T>(queryUuid);
    }

    public async Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var dbResponse = await SendRequestAsync(
                "merge",
                [data.Id, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [recordId, patches],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [recordId, patches],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [table, patches],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var dbResponse = await SendRequestAsync(
                "query",
                [query, parameters],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

#if NET7_0_OR_GREATER
        var executionTime = Stopwatch.GetElapsedTime(executionStartTime);
#else
        long executionEndTime = Stopwatch.GetTimestamp();
        var executionTime = TimeSpan.FromTicks(executionEndTime - executionStartTime);
#endif

        _surrealDbLoggerFactory?.Query?.LogQuerySuccess(
            query,
            SurrealDbLoggerExtensions.FormatQueryParameters(
                parameters,
                _parameters.Logging.SensitiveDataLoggingEnabled
            ),
            SurrealDbLoggerExtensions.FormatExecutionTime(executionTime)
        );

        var list = dbResponse.GetValue<List<ISurrealDbResult>>() ?? [];
        return new SurrealDbResponse(list);
    }

    public async Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [ins, table, outs, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<TOutput> Relate<TOutput, TData>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [@in, recordId, @out, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Run<T>(
        string name,
        string? version,
        object[]? args,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "run",
                [name, version, args],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [table],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>()!;
    }

    public async Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [recordId],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [recordId],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        CancellationToken cancellationToken
    )
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        var dbResponse = await SendRequestAsync(
                "select",
                [recordIdRange],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>()!;
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

        await SendRequestAsync(
                "let",
                [key, value],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public Task SignIn(RootAuth rootAuth, CancellationToken cancellationToken)
    {
        return SignIn(rootAuth, SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task SignIn(
        RootAuth rootAuth,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync("signin", [rootAuth], priority, cancellationToken)
            .ConfigureAwait(false);

        _config.SetBasicAuth(rootAuth.Username, rootAuth.Password);
    }

    public async Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [nsAuth],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
    }

    public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [dbAuth],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
    }

    public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [scopeAuth],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
    }

    public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync(
                "signup",
                [scopeAuth],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
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

    public async Task<bool> TryResetAsync()
    {
        try
        {
            // Cancel all response tasks
            foreach (var (key, value) in _responseTaskHandler)
            {
                _responseTaskHandler.TryRemove(key, out _);
                value.TrySetCanceled();
            }

            // Clear server context
            await Clear(default).ConfigureAwait(false);

            // Reset configuration
            _config.Reset(_parameters);

            // Apply configuration for connection reuse (neutral state)
            await ApplyConfigurationAsync(default).ConfigureAwait(false);

            // Close Live Queries
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
                            SurrealDbLiveQueryClosureReason.ConnectionTerminated
                        );
                        endChannelsTasks.Add(closeTask);
                    }
                }
            }

            if (endChannelsTasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(endChannelsTasks).ConfigureAwait(false);
                }
                catch (SurrealDbException) { }
                catch (OperationCanceledException) { }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
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

        await SendRequestAsync("unset", [key], SurrealDbWsRequestPriority.Normal, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> Update<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbException("Cannot update a record without an Id");

        var dbResponse = await SendRequestAsync(
                "update",
                [data.Id, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        var dbResponse = await SendRequestAsync(
                "update",
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "update",
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        var dbResponse = await SendRequestAsync(
                "update",
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot upsert a record without an Id");

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [data.Id, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : class
    {
        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [table, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [recordId, data],
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        return Use(ns, db, SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task Use(
        string ns,
        string db,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync("use", [ns, db], priority, cancellationToken).ConfigureAwait(false);
        _config.Use(ns, db);
    }

    public Task<string> Version(CancellationToken cancellationToken)
    {
        return Version(SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task<string> Version(
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync("version", null, priority, cancellationToken)
            .ConfigureAwait(false);
        string version = dbResponse.GetValue<string>()!;

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private CborOptions GetCborOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(
            _parameters.NamingPolicy,
            _configureCborOptions
        );
    }

    private async Task ApplyConfigurationAsync(CancellationToken cancellationToken)
    {
        if (_config.Ns is not null)
        {
            await Use(_config.Ns, _config.Db!, SurrealDbWsRequestPriority.High, cancellationToken)
                .ConfigureAwait(false);

            if (_config.Db is not null)
            {
                _surrealDbLoggerFactory?.Connection?.LogConnectionNamespaceAndDatabaseSet(
                    _config.Ns,
                    _config.Db
                );
            }
            else
            {
                _surrealDbLoggerFactory?.Connection?.LogConnectionNamespaceSet(_config.Ns);
            }
        }

        if (_config.Auth is BasicAuth basicAuth)
        {
            await SignIn(
                    new RootAuth { Username = basicAuth.Username, Password = basicAuth.Password! },
                    SurrealDbWsRequestPriority.High,
                    cancellationToken
                )
                .ConfigureAwait(false);

            _surrealDbLoggerFactory?.Connection?.LogConnectionSignedAsRoot(
                SurrealDbLoggerExtensions.FormatParameterValue(
                    basicAuth.Username,
                    _parameters.Logging.SensitiveDataLoggingEnabled
                ),
                SurrealDbLoggerExtensions.FormatParameterValue(
                    basicAuth.Password,
                    _parameters.Logging.SensitiveDataLoggingEnabled
                )
            );
        }

        if (_config.Auth is BearerAuth bearerAuth)
        {
            await Authenticate(
                    new Jwt(bearerAuth.Token),
                    SurrealDbWsRequestPriority.High,
                    cancellationToken
                )
                .ConfigureAwait(false);

            _surrealDbLoggerFactory?.Connection?.LogConnectionSignedViaJwt(
                SurrealDbLoggerExtensions.FormatParameterValue(
                    bearerAuth.Token,
                    _parameters.Logging.SensitiveDataLoggingEnabled
                )
            );
        }
    }

    private async Task Ping(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
        {
            await SendRequestAsync("ping", null, SurrealDbWsRequestPriority.High, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Avoid multiple connections in a multi-threading context
    /// and prevent usage before initialized
    /// </summary>
    internal async Task InternalConnectAsync(
        bool requireInitialized,
        CancellationToken cancellationToken
    )
    {
        if (!_wsClient.IsStarted || (requireInitialized && !_isInitialized))
        {
            try
            {
                await _semaphoreConnect.WaitAsync(cancellationToken).ConfigureAwait(false);

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
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cancellationToken.Register(timeoutCts.Cancel);

        bool requireInitialized = priority == SurrealDbWsRequestPriority.Normal;

        try
        {
            await InternalConnectAsync(requireInitialized, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timeoutCts.Dispose();

            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException();

            throw;
        }

        var taskCompletionSource = new SurrealWsTaskCompletionSource(priority);
        timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_responseTaskHandler.TryAdd(id, priority, taskCompletionSource));

        /*var waitUntilTask = _responseTaskHandler.WaitUntilAsync(priority, cancellationToken);

        var initialTask = await Task.WhenAny(waitUntilTask, timeoutTask).ConfigureAwait(false);

        if (initialTask != waitUntilTask)
        {
            _responseTaskHandler.TryRemove(id, out _);
            taskCompletionSource.TrySetCanceled(CancellationToken.None);
            throw new TimeoutException();
        }*/

        bool shouldSendParamsInRequest = parameters is not null && parameters.Length > 0;

        var request = new SurrealDbWsRequest
        {
            Id = id,
            Method = method,
            Parameters = shouldSendParamsInRequest ? parameters : null,
        };

        await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();

        try
        {
            await CborSerializer
                .SerializeAsync(request, stream, GetCborOptions(), timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timeoutCts.Dispose();

            _responseTaskHandler.TryRemove(id, out _);
            if (!cancellationToken.IsCancellationRequested)
            {
                _surrealDbLoggerFactory?.Method?.LogRequestFailed(id, "Timeout");
                throw new TimeoutException();
            }

            throw;
        }
        catch
        {
            timeoutCts.Dispose();
            _responseTaskHandler.TryRemove(id, out _);
            throw;
        }

        bool canGetBuffer = stream.TryGetBuffer(out var payload);
        bool isMessageSent = canGetBuffer && _wsClient.Send(payload);

        if (!isMessageSent)
        {
            timeoutCts.Dispose();

            _responseTaskHandler.TryRemove(id, out _);
            taskCompletionSource.TrySetCanceled(CancellationToken.None);
            _surrealDbLoggerFactory?.Method?.LogRequestFailed(id, "Failed to send message");
            throw new SurrealDbException("Failed to send message");
        }

        try
        {
#if NET7_0_OR_GREATER
            var executionTime = Stopwatch.GetElapsedTime(executionStartTime);
#else
            long executionEndTime = Stopwatch.GetTimestamp();
            var executionTime = TimeSpan.FromTicks(executionEndTime - executionStartTime);
#endif

            _surrealDbLoggerFactory?.Method?.LogRequestSuccess(
                id,
                method,
                SurrealDbLoggerExtensions.FormatRequestParameters(
                    parameters!,
                    _parameters.Logging.SensitiveDataLoggingEnabled
                ),
                SurrealDbLoggerExtensions.FormatExecutionTime(executionTime)
            );

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timeoutCts.Dispose();

            _responseTaskHandler.TryRemove(id, out _);
            if (!cancellationToken.IsCancellationRequested)
            {
                _surrealDbLoggerFactory?.Method?.LogRequestFailed(id, "Timeout");
                throw new TimeoutException();
            }

            throw;
        }
        catch
        {
            timeoutCts.Dispose();
            _responseTaskHandler.TryRemove(id, out _);
            throw;
        }
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
