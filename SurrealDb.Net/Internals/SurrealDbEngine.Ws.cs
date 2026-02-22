using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Sessions;
using SurrealDb.Net.Internals.Stream;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using Websocket.Client;
#if NET9_0_OR_GREATER
using ConcurrentCollections;
#endif
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif

namespace SurrealDb.Net.Internals;

internal sealed class SurrealDbWsEngine : ISurrealDbEngine
{
    private static readonly ConcurrentDictionary<string, SurrealDbWsEngine> _wsEngines = new();

    internal SemVersion? _version { get; private set; }
    internal Action<CborOptions>? _configureCborOptions { get; }

#if DEBUG
    public string Id => _id;
#endif

    private readonly string _id;
    private readonly SurrealDbOptions _parameters;
    private readonly ISurrealDbLoggerFactory? _surrealDbLoggerFactory;
    private readonly ISessionizer? _sessionizer;
    private readonly WebsocketClient _wsClient;
    private readonly IDisposable _receiverSubscription;
    private readonly ConcurrentDictionary<
        Guid,
        SurrealDbLiveQueryChannelSubscriptions
    > _liveQueryChannelSubscriptionsPerQuery = new();
    private readonly Pinger _pinger;
    private readonly SemaphoreSlim _semaphoreConnect = new(1, 1);

#if NET9_0_OR_GREATER
    private static readonly ConcurrentHashSet<string> _allResponseTaskIds = [];
    private static readonly SurrealDbWsSendRequestChannel _sendRequestChannel = new();

    private readonly ConcurrentDictionary<string, SurrealWsTaskCompletionSource> _responseTasks =
        new();
#else
    private readonly WsResponseTaskHandler _responseTaskHandler;
#endif

    private bool _isInitialized;

    public Uri Uri { get; }
    public RpcSessionInfos SessionInfos { get; } = new();

#if NET9_0_OR_GREATER
    static SurrealDbWsEngine()
    {
        // Sender subscriptions
        Task.Run(async () =>
        {
            try
            {
                await foreach (var request in _sendRequestChannel.ReadAllAsync())
                {
                    try
                    {
                        await SendInnerRequestAsync(request).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        // Prevent sender from failure & log error
                        if (request.WsEngine.TryGetTarget(out var engine))
                        {
                            engine._surrealDbLoggerFactory?.Method?.LogRequestFailed(
                                request.Content.Id,
                                e.Message
                            );
                        }
                    }
                }
            }
            catch
            {
                // TODO : Retry on failure?
                // TODO : Log the exception?
            }
        });
    }
#endif

    public SurrealDbWsEngine(
        SurrealDbOptions parameters,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory,
        ISessionizer? sessionizer
    )
    {
        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
        } while (!_wsEngines.TryAdd(id, this));

        Uri = new Uri(parameters.Endpoint!);
        _id = id;
        _parameters = parameters;
        _configureCborOptions = configureCborOptions;
        _surrealDbLoggerFactory = surrealDbLoggerFactory;
        _sessionizer = sessionizer;

        // Set root session
        SessionInfos.Set(null, new RpcSessionInfo(parameters));

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
#if !NET9_0_OR_GREATER
        _responseTaskHandler = new(id);
#endif

        _receiverSubscription = _wsClient
            .MessageReceived.ObserveOn(TaskPoolScheduler.Default)
            .Select(message =>
                Observable.FromAsync(
                    async (cancellationToken) =>
                    {
#if DEBUG
                        try
                        {
#endif
                        ISurrealDbWsResponse? response = null;

                        if (message.MessageType == WebSocketMessageType.Binary)
                        {
                            using var stream =
                                message.Stream
                                ?? MemoryStreamProvider.MemoryStreamManager.GetStream(
                                    message.Binary!
                                );

                            if (
                                _surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug)
                                == true
                            )
                            {
                                string cborData = CborDebugHelper.CborBinaryToHexa(stream);
                                _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(
                                    cborData
                                );
                            }

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
#if NET9_0_OR_GREATER
                            && _responseTasks.TryRemove(
                                surrealDbWsStandardResponse.Id,
                                out var responseTaskCompletionSource
                            )
#else
                            && _responseTaskHandler.TryRemove(
                                surrealDbWsStandardResponse.Id,
                                out var responseTaskCompletionSource
                            )
#endif
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
#if DEBUG
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            throw;
                        }
#endif
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
                        await ApplyReconnectConfigurationAsync(cancellationToken)
                            .ConfigureAwait(false);
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

    public async Task Attach(Guid sessionId, CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        await SendRequestAsync(
                "attach",
                null,
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public Task Authenticate(Tokens tokens, Guid? sessionId, CancellationToken cancellationToken)
    {
        return Authenticate(
            tokens,
            SurrealDbWsRequestPriority.Normal,
            sessionId,
            cancellationToken
        );
    }

    private async Task Authenticate(
        Tokens tokens,
        SurrealDbWsRequestPriority priority,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync(
                "authenticate",
                [tokens.Access],
                sessionId,
                priority,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task CloseSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);
        await Detach(sessionId, cancellationToken).ConfigureAwait(false);

        SessionInfos.Remove(sessionId);
    }

    public async Task<Guid> CreateSession(CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        var newId = Guid.NewGuid();

        await Attach(newId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(newId, new RpcSessionInfo());

        return newId;
    }

    public async Task<Guid> CreateSession(Guid from, CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        var newId = Guid.NewGuid();
        var newState = SessionInfos.Clone(from, newId);

        await Attach(newId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(newId, newState);

        return newId;
    }

    private async Task CreateSession(
        Guid sessionId,
        RpcSessionInfo sessionInfo,
        CancellationToken cancellationToken
    )
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        await Attach(sessionId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(sessionId, sessionInfo);

        await ApplyConfigurationAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
            throw new SurrealDbException("Client already started");

        _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters.Endpoint!);

        _isInitialized = false;

        await _wsClient.StartOrFail().ConfigureAwait(false);

        await ApplyRootConfigurationAsync(cancellationToken).ConfigureAwait(false);

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

    public async Task<T> Create<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var dbResponse = await SendRequestAsync(
                "create",
                [data.Id, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Create<T>(
        string table,
        T? data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "create",
                [table, data],
                sessionId,
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
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "create",
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task Delete(string table, Guid? sessionId, CancellationToken cancellationToken)
    {
        await SendRequestAsync(
                "delete",
                [table],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(
        RecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "delete",
                [recordId],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public async Task<bool> Delete(
        StringRecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "delete",
                [recordId],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public async Task Detach(Guid sessionId, CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        await SendRequestAsync(
                "detach",
                null,
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
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

                    var killTask = Kill(
                        key,
                        SurrealDbLiveQueryClosureReason.SocketClosed,
                        null,
                        default
                    );
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

#if NET9_0_OR_GREATER
        foreach (var (key, value) in _responseTasks)
        {
            _responseTasks.TryRemove(key, out _);
            value.TrySetCanceled();
        }
#else
        foreach (var (key, value) in _responseTaskHandler)
        {
            _responseTaskHandler.TryRemove(key, out _);
            value.TrySetCanceled();
        }
#endif
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

    public async Task<T> Info<T>(Guid? sessionId, CancellationToken cancellationToken)
    {
        var dbResponse = await SendRequestAsync(
                "info",
                null,
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "insert",
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> InsertRelation<T>(
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbException("Cannot create a relation record without an Id");

        var dbResponse = await SendRequestAsync(
                "insert_relation",
                [null, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
    }

    public async Task<T> InsertRelation<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        if (data.Id is not null)
            throw new SurrealDbException(
                "You cannot provide both the table and an Id for the record. Either use the method overload without 'table' param or set the Id property to null."
            );

        var dbResponse = await SendRequestAsync(
                "insert_relation",
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
    }

    public async Task Invalidate(Guid? sessionId, CancellationToken cancellationToken)
    {
        await SendRequestAsync(
                "invalidate",
                null,
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.ResetAuth();
    }

    public async Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        Guid? sessionId,
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
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid, Guid? sessionId)
    {
        _liveQueryChannelSubscriptionsPerQuery.TryAdd(queryUuid, new(_id));
        return new SurrealDbLiveQuery<T>(queryUuid, this, sessionId);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await RawQuery(query, parameters, sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (dbResponse.HasErrors)
        {
            throw new SurrealDbErrorResultException(dbResponse.FirstError!);
        }

        if (dbResponse.FirstOk is null)
        {
            throw new SurrealDbErrorResultException();
        }

        var queryUuid = dbResponse.FirstOk.GetValue<Guid>()!;

        return ListenLive<T>(queryUuid, sessionId);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "live",
                [table, diff],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var queryUuid = dbResponse.GetValue<Guid>()!;

        return ListenLive<T>(queryUuid, sessionId);
    }

    public async Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TMerge : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var dbResponse = await SendRequestAsync(
                "merge",
                [data.Id, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [recordId, patches],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [recordId, patches],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [table, patches],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var dbResponse = await SendRequestAsync(
                "query",
                [query, parameters],
                sessionId,
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
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [ins, table, outs, data],
                sessionId,
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
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [@in, recordId, @out, data],
                sessionId,
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
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "run",
                [name, version, args],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Select<T>(
        string table,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [table],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>()!;
    }

    public async Task<T?> Select<T>(
        RecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [recordId],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<T?> Select<T>(
        StringRecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [recordId],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        var dbResponse = await SendRequestAsync(
                "select",
                [recordIdRange],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>()!;
    }

    public async Task<IEnumerable<Guid>> Sessions(CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        var dbResponse = await SendRequestAsync(
                "sessions",
                null,
                null,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<Guid>()!;
    }

    public async Task Set(
        string key,
        object value,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
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
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public Task SignIn(RootAuth rootAuth, Guid? sessionId, CancellationToken cancellationToken)
    {
        return SignIn(rootAuth, sessionId, SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task SignIn(
        SystemAuth systemAuth,
        Guid? sessionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync("signin", [systemAuth], sessionId, priority, cancellationToken)
            .ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.SetSystemAuth(systemAuth);
    }

    public async Task<Tokens> SignIn(
        NamespaceAuth nsAuth,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [nsAuth],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var tokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetSystemAuth(nsAuth);

        return tokens;
    }

    public async Task<Tokens> SignIn(
        DatabaseAuth dbAuth,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [dbAuth],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var tokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetSystemAuth(dbAuth);

        return tokens;
    }

    public async Task<Tokens> SignIn<T>(
        T scopeAuth,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [scopeAuth],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var tokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetBearerAuth(tokens.Access);

        return tokens;
    }

    public async Task<Tokens> SignUp<T>(
        T scopeAuth,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync(
                "signup",
                [scopeAuth],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var tokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetBearerAuth(tokens.Access);

        return tokens;
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

    public async Task Unset(string key, Guid? sessionId, CancellationToken cancellationToken)
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
                "unset",
                [key],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Update<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbException("Cannot update a record without an Id");

        var dbResponse = await SendRequestAsync(
                "update",
                [data.Id, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        var dbResponse = await SendRequestAsync(
                "update",
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "update",
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<IEnumerable<TOutput>> Update<TData, TOutput>(
        string table,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "update",
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        var dbResponse = await SendRequestAsync(
                "update",
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Upsert<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot upsert a record without an Id");

        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [data.Id, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<IEnumerable<TOutput>> Upsert<TData, TOutput>(
        string table,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [table, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [recordId, data],
                sessionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public Task Use(string ns, string db, Guid? sessionId, CancellationToken cancellationToken)
    {
        return Use(ns, db, sessionId, SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task Use(
        string ns,
        string db,
        Guid? sessionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync("use", [ns, db], sessionId, priority, cancellationToken)
            .ConfigureAwait(false);
        SessionInfos.Get(sessionId)?.Use(ns, db);
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
        var dbResponse = await SendRequestAsync("version", null, null, priority, cancellationToken)
            .ConfigureAwait(false);
        string version = dbResponse.GetValue<string>()!;

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private CborOptions GetCborOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(_configureCborOptions);
    }

    private async Task ApplyReconnectConfigurationAsync(CancellationToken cancellationToken)
    {
        foreach (var sessionId in SessionInfos.Enumerate())
        {
            await ApplyConfigurationAsync(sessionId, cancellationToken);
        }
    }

    private Task ApplyRootConfigurationAsync(CancellationToken cancellationToken)
    {
        return ApplyConfigurationAsync(null, cancellationToken);
    }

    private async Task ApplyConfigurationAsync(Guid? sessionId, CancellationToken cancellationToken)
    {
        var session = SessionInfos.Get(sessionId)!;

        if (session.Ns is not null)
        {
            await Use(
                    session.Ns,
                    session.Db!,
                    sessionId,
                    SurrealDbWsRequestPriority.High,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (session.Db is not null)
            {
                _surrealDbLoggerFactory?.Connection?.LogConnectionNamespaceAndDatabaseSet(
                    session.Ns,
                    session.Db
                );
            }
            else
            {
                _surrealDbLoggerFactory?.Connection?.LogConnectionNamespaceSet(session.Ns);
            }
        }

        if (session.Auth is InternalSystemAuth systemAuth)
        {
            if (systemAuth.Auth is RootAuth rootAuth)
            {
                await SignIn(
                        rootAuth,
                        sessionId,
                        SurrealDbWsRequestPriority.High,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                _surrealDbLoggerFactory?.Connection?.LogConnectionSignedAsRoot(
                    SurrealDbLoggerExtensions.FormatParameterValue(
                        rootAuth.Username,
                        _parameters.Logging.SensitiveDataLoggingEnabled
                    ),
                    SurrealDbLoggerExtensions.FormatParameterValue(
                        rootAuth.Password,
                        _parameters.Logging.SensitiveDataLoggingEnabled
                    )
                );
            }
            if (systemAuth.Auth is NamespaceAuth nsAuth)
            {
                await SignIn(nsAuth, sessionId, SurrealDbWsRequestPriority.High, cancellationToken)
                    .ConfigureAwait(false);

                _surrealDbLoggerFactory?.Connection?.LogConnectionSignedAsNamespaceUser(
                    SurrealDbLoggerExtensions.FormatParameterValue(
                        nsAuth.Username,
                        _parameters.Logging.SensitiveDataLoggingEnabled
                    ),
                    SurrealDbLoggerExtensions.FormatParameterValue(
                        nsAuth.Password,
                        _parameters.Logging.SensitiveDataLoggingEnabled
                    )
                );
            }
            if (systemAuth.Auth is DatabaseAuth dbAuth)
            {
                await SignIn(dbAuth, sessionId, SurrealDbWsRequestPriority.High, cancellationToken)
                    .ConfigureAwait(false);

                _surrealDbLoggerFactory?.Connection?.LogConnectionSignedAsDatabaseUser(
                    SurrealDbLoggerExtensions.FormatParameterValue(
                        dbAuth.Username,
                        _parameters.Logging.SensitiveDataLoggingEnabled
                    ),
                    SurrealDbLoggerExtensions.FormatParameterValue(
                        dbAuth.Password,
                        _parameters.Logging.SensitiveDataLoggingEnabled
                    )
                );
            }
        }

        if (session.Auth is BearerAuth bearerAuth)
        {
            await Authenticate(
                    new Tokens(bearerAuth.Token),
                    SurrealDbWsRequestPriority.High,
                    sessionId,
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
            await SendRequestAsync(
                    "ping",
                    null,
                    null,
                    SurrealDbWsRequestPriority.High,
                    cancellationToken
                )
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

    private async Task EnsureVersionIsSetAsync(CancellationToken cancellationToken)
    {
        if (_version is not null)
            return;

        await Version(cancellationToken).ConfigureAwait(false);
    }

    private async Task RequireMajorVersion(int version, CancellationToken cancellationToken)
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < version)
            throw new NotImplementedException();
    }

    private async Task<SurrealDbWsOkResponse> SendRequestAsync(
        string method,
        object?[]? parameters,
        Guid? sessionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var registration = cancellationToken.Register(timeoutCts.Cancel);

        bool requireInitialized = priority == SurrealDbWsRequestPriority.Normal;

        try
        {
            await InternalConnectAsync(requireInitialized, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException();

            throw;
        }

        if (
            sessionId.HasValue
            && _sessionizer is not null
            && _sessionizer.Get(sessionId.Value, out var newSessionInfo)
            && newSessionInfo is RpcSessionInfo newRpcSessionInfo
        )
        {
            _sessionizer.TryRemove(sessionId.Value);
            await CreateSession(sessionId.Value, newRpcSessionInfo, cancellationToken)
                .ConfigureAwait(false);
        }

#if NET9_0_OR_GREATER
        var taskCompletionSource = new SurrealWsTaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
#else
        var taskCompletionSource = new SurrealWsTaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously,
            priority
        );
#endif
        await using var cancelRegistration = timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        string id;

        // 💡 Ensures unique id (no collision)
        do
        {
            id = RandomHelper.CreateRandomId();
#if NET9_0_OR_GREATER
        } while (!_allResponseTaskIds.Add(id));
        _responseTasks.TryAdd(id, taskCompletionSource);
#else
        } while (!_responseTaskHandler.TryAdd(id, priority, taskCompletionSource));
#endif

#if !NET9_0_OR_GREATER
        await _responseTaskHandler.WaitUntilAsync(priority).ConfigureAwait(false);
#endif
        bool shouldSendParamsInRequest = parameters is not null && parameters.Length > 0;
        var innerRequest = new SurrealDbWsRequest
        {
            Id = id,
            Method = method,
            Parameters = shouldSendParamsInRequest ? parameters : null,
            SessionId = sessionId,
        };

        await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();

        var request = new SurrealDbWsSendRequest(
            innerRequest,
            priority,
            taskCompletionSource,
            stream,
            new WeakReference<SurrealDbWsEngine>(this),
            timeoutCts.Token
        );

        try
        {
#if NET9_0_OR_GREATER
            await _sendRequestChannel.WriteAsync(request, timeoutCts.Token).ConfigureAwait(false);
#else
            await SendInnerRequestAsync(request).ConfigureAwait(false);
#endif

            var result = await taskCompletionSource.Task.ConfigureAwait(false);

#if NET7_0_OR_GREATER
            var executionTime = Stopwatch.GetElapsedTime(executionStartTime);
#else
            long executionEndTime = Stopwatch.GetTimestamp();
            var executionTime = TimeSpan.FromTicks(executionEndTime - executionStartTime);
#endif

            _surrealDbLoggerFactory?.Method?.LogRequestSuccess(
                id,
                request.Content.Method,
                SurrealDbLoggerExtensions.FormatRequestParameters(
                    request.Content.Parameters!,
                    _parameters.Logging.SensitiveDataLoggingEnabled
                ),
                SurrealDbLoggerExtensions.FormatExecutionTime(executionTime)
            );

            return result;
        }
        catch (MessageNotSentSurrealDbException)
        {
            request.CompletionSource.TrySetCanceled(CancellationToken.None);
            _surrealDbLoggerFactory?.Method?.LogRequestFailed(id, "Failed to send message");

            throw;
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _surrealDbLoggerFactory?.Method?.LogRequestFailed(id, "Timeout");
                throw new TimeoutException();
            }

            throw;
        }
        finally
        {
#if NET9_0_OR_GREATER
            _responseTasks.TryRemove(id, out _);
            _allResponseTaskIds.TryRemove(id);
#else
            _responseTaskHandler.TryRemove(id, out _);
#endif
        }
    }

    private static async Task SendInnerRequestAsync(SurrealDbWsSendRequest request)
    {
        if (!request.WsEngine.TryGetTarget(out var wsEngine))
        {
            request.CompletionSource.SetException(new EngineDisposedSurrealDbException());
            return;
        }

        try
        {
            await CborSerializer
                .SerializeAsync(
                    request.Content,
                    request.Stream,
                    wsEngine.GetCborOptions(),
                    request.CancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            request.CompletionSource.SetException(ex);
            return;
        }

        if (wsEngine._surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
        {
            request.Stream.TryGetBuffer(out var streamData);
            string cborData = CborDebugHelper.CborBinaryToHexa(streamData);
            wsEngine._surrealDbLoggerFactory?.Serialization?.LogSerializationDataSerialized(
                cborData
            );
        }

        bool canGetBuffer = request.Stream.TryGetBuffer(out var payload);
        bool isMessageSent = canGetBuffer && wsEngine._wsClient.Send(payload);

        if (!isMessageSent)
        {
            request.CompletionSource.SetException(new MessageNotSentSurrealDbException());
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
