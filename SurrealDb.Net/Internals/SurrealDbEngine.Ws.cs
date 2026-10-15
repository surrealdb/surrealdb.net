using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Exceptions.LiveQuery;
using SurrealDb.Net.Exceptions.Methods;
using SurrealDb.Net.Exceptions.Response;
using SurrealDb.Net.Exceptions.Rpc;
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

    /// <summary>
    /// Live queries that can be started again after a reconnection, by the id first given by the server.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, WsLiveQueryDefinition> _liveQueryDefinitions =
        new();

    /// <summary>
    /// The id first given by the server for a live query, by the id of its current subscription.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, Guid> _liveQueryIdsByServerId = new();

    /// <summary>
    /// Completed once the connection state (sessions, auth, variables, live queries) is replayed after a reconnection.
    /// Normal requests wait for it, so they never run on a half-restored connection.
    /// </summary>
    private TaskCompletionSource<bool> _replayCompletion = CreateCompletedReplay();
    private volatile bool _disposing;

#if NET9_0_OR_GREATER
    private static readonly ConcurrentHashSet<string> _allResponseTaskIds = [];
    private static readonly SurrealDbWsSendRequestChannel _sendRequestChannel = new();

    private readonly ConcurrentDictionary<string, SurrealWsTaskCompletionSource> _responseTasks =
        new();
#else
    private readonly WsResponseTaskHandler _responseTaskHandler;
#endif

    /// <summary>
    /// Open streaming queries, by request id.
    /// </summary>
    private readonly ConcurrentDictionary<string, Channel<SurrealDbWsOkResponse>> _queryStreams =
        new();

    private static readonly SemVersion QueryStreamMinVersion = new(3, 3, 0);

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
            {
                // 💡 Messages are handled concurrently, but the frames of a streaming query must keep their order:
                // while a stream is open, messages are read here, one after the other
                if (!_queryStreams.IsEmpty)
                {
                    ISurrealDbWsResponse? response;
                    try
                    {
                        response = DeserializeMessage(message);

                        if (TryRouteQueryStreamResponse(response))
                        {
                            return Observable.Empty<Unit>();
                        }
                    }
                    catch (Exception ex)
                    {
                        OnReceivedMessageProcessingFailed(ex);
                        return Observable.Empty<Unit>();
                    }

                    return Observable.FromAsync(_ => HandleResponseAsync(response));
                }

                return Observable.FromAsync(
                    async (cancellationToken) =>
                    {
                        ISurrealDbWsResponse? response;
                        try
                        {
                            response = await DeserializeMessageAsync(message, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            OnReceivedMessageProcessingFailed(ex);
                            return;
                        }

                        await HandleResponseAsync(response).ConfigureAwait(false);
                    }
                );
            })
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

        _wsClient.DisconnectionHappened.Subscribe(OnDisconnection);
    }

    public Task Attach(Guid sessionId, CancellationToken cancellationToken)
    {
        return Attach(sessionId, SurrealDbWsRequestPriority.Normal, cancellationToken);
    }

    private async Task Attach(
        Guid sessionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        await SendRequestAsync("attach", null, sessionId, null, priority, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task Authenticate(
        Tokens tokens,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        return Authenticate(
            tokens,
            SurrealDbWsRequestPriority.Normal,
            sessionId,
            transactionId,
            cancellationToken
        );
    }

    private async Task Authenticate(
        Tokens tokens,
        SurrealDbWsRequestPriority priority,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync(
                "authenticate",
                [tokens.Access],
                sessionId,
                transactionId,
                priority,
                cancellationToken
            )
            .ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.SetBearerAuth(tokens.Access);
    }

    public async Task<Guid> Begin(Guid? sessionId, CancellationToken cancellationToken)
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        var response = await SendRequestAsync(
                "begin",
                null,
                sessionId,
                null,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        var transactionId = response.GetValue<Guid>();

        return transactionId;
    }

    public async Task Cancel(
        Guid? sessionId,
        Guid transactionId,
        CancellationToken cancellationToken
    )
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);
        await WaitForTransactionStreamsAsync(transactionId, cancellationToken)
            .ConfigureAwait(false);

        await SendRequestAsync(
                "cancel",
                [transactionId],
                sessionId,
                null,
                SurrealDbWsRequestPriority.Normal,
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

    public async Task Commit(
        Guid? sessionId,
        Guid transactionId,
        CancellationToken cancellationToken
    )
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);
        await WaitForTransactionStreamsAsync(transactionId, cancellationToken)
            .ConfigureAwait(false);

        await SendRequestAsync(
                "commit",
                [transactionId],
                sessionId,
                null,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
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
        var fromState = SessionInfos.Get(from) ?? throw new InvalidSessionException(from);

        var newId = Guid.NewGuid();
        await CreateSession(newId, new RpcSessionInfo(fromState), cancellationToken)
            .ConfigureAwait(false);

        return newId;
    }

    /// <summary>
    /// Attaches a new session on the server, then replays the given state on it (namespace, database and auth).
    /// </summary>
    private async Task CreateSession(
        Guid sessionId,
        RpcSessionInfo sessionInfo,
        CancellationToken cancellationToken
    )
    {
        await RequireMajorVersion(3, cancellationToken).ConfigureAwait(false);

        await Attach(sessionId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(sessionId, sessionInfo);

        try
        {
            await ApplyConfigurationAsync(sessionId, null, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            SessionInfos.Remove(sessionId);
            try
            {
                await Detach(sessionId, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // 💡 The original failure matters more than a failed cleanup
            }

            throw;
        }
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_wsClient.IsStarted)
            throw new SurrealDbConnectException("Client already started");

        _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters.Endpoint!);

        _isInitialized = false;

        await _wsClient.StartOrFail().ConfigureAwait(false);

        await ApplyRootConfigurationAsync(cancellationToken).ConfigureAwait(false);

        string version = await Version(SurrealDbWsRequestPriority.High, cancellationToken)
            .ConfigureAwait(false);
        _version = version.ToSemver();

        if (_version.CompareSortOrderTo(new SemVersion(1, 4, 0)) < 0)
        {
            throw new SurrealDbConnectException(
                "CBOR is only supported on SurrealDB 1.4.0 or later."
            );
        }

        _pinger.Start();
        _isInitialized = true;
        _replayCompletion.TrySetResult(true);

        _surrealDbLoggerFactory?.Connection?.LogConnectionSuccess(_parameters.Endpoint!);
    }

    public async Task<T> Create<T>(
        T data,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbMethodException("Cannot create a record without an Id");

        var dbResponse = await SendRequestAsync(
                "create",
                [data.Id, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "create",
                [table, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "create",
                [recordId, data],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task Delete(
        string table,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync(
                "delete",
                [table],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(
        RecordId recordId,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "delete",
                [recordId],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public async Task<bool> Delete(
        StringRecordId recordId,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "delete",
                [recordId],
                sessionId,
                transactionId,
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
                null,
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

        _disposing = true;
        _replayCompletion.TrySetCanceled();
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
        foreach (var (key, value) in _queryStreams)
        {
            _queryStreams.TryRemove(key, out _);
            value.Writer.TryComplete(new EngineDisposedSurrealDbException());
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

    public async Task<T> Info<T>(
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "info",
                null,
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "insert",
                [table, data],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T> InsertRelation<T>(
        T data,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbMethodException("Cannot create a relation record without an Id");

        var dbResponse = await SendRequestAsync(
                "insert_relation",
                [null, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        if (data.Id is not null)
            throw new SurrealDbMethodException(
                "You cannot provide both the table and an Id for the record. Either use the method overload without 'table' param or set the Id property to null."
            );

        var dbResponse = await SendRequestAsync(
                "insert_relation",
                [table, data],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
    }

    public async Task Invalidate(
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync(
                "invalidate",
                null,
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        // 💡 A resubscribed live query has a new id on the server
        var serverQueryUuid = _liveQueryDefinitions.TryRemove(queryUuid, out var definition)
            ? definition.ServerId
            : queryUuid;
        _liveQueryIdsByServerId.TryRemove(serverQueryUuid, out _);

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
                [serverQueryUuid],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid, Guid? sessionId, Guid? transactionId)
    {
        _liveQueryChannelSubscriptionsPerQuery.TryAdd(queryUuid, new(_id));
        return new SurrealDbLiveQuery<T>(queryUuid, this, sessionId, transactionId);
    }

    public async Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var queryUuid = await StartLiveRawQuery(
                query,
                parameters,
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!transactionId.HasValue)
        {
            _liveQueryDefinitions[queryUuid] = new()
            {
                ServerId = queryUuid,
                SessionId = sessionId,
                Query = query,
                Parameters = parameters,
            };
        }

        return ListenLive<T>(queryUuid, sessionId, transactionId);
    }

    private async Task<Guid> StartLiveRawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await RawQuery(
                query,
                parameters,
                sessionId,
                transactionId,
                priority,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (dbResponse.HasErrors)
        {
            throw new SurrealDbErrorResultException(dbResponse.FirstError!);
        }

        if (dbResponse.FirstOk is null)
        {
            throw new SurrealDbErrorResultException();
        }

        return dbResponse.FirstOk.GetValue<Guid>()!;
    }

    public async Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var queryUuid = await StartLiveTable(
                table,
                diff,
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!transactionId.HasValue)
        {
            _liveQueryDefinitions[queryUuid] = new()
            {
                ServerId = queryUuid,
                SessionId = sessionId,
                Table = table,
                Diff = diff,
            };
        }

        return ListenLive<T>(queryUuid, sessionId, transactionId);
    }

    private async Task<Guid> StartLiveTable(
        string table,
        bool diff,
        Guid? sessionId,
        Guid? transactionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "live",
                [table, diff],
                sessionId,
                transactionId,
                priority,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<Guid>()!;
    }

    public async Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TMerge : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbMethodException("Cannot create a record without an Id");

        var dbResponse = await SendRequestAsync(
                "merge",
                [data.Id, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [recordId, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [recordId, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [table, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "merge",
                [table, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [recordId, patches],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [recordId, patches],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "patch",
                [table, patches],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        return RawQuery(
            query,
            parameters,
            sessionId,
            transactionId,
            SurrealDbWsRequestPriority.Normal,
            cancellationToken
        );
    }

    private async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var dbResponse = await SendRequestAsync(
                "query",
                [query, parameters],
                sessionId,
                transactionId,
                priority,
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

    public IAsyncEnumerable<SurrealDbQueryStreamFrame> RawQueryStream(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        return RawQueryStreamAsync(query, parameters, sessionId, transactionId, cancellationToken);
    }

    private async IAsyncEnumerable<SurrealDbQueryStreamFrame> RawQueryStreamAsync(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (
            _version is null
            || _version.WithoutPrereleaseOrMetadata().CompareSortOrderTo(QueryStreamMinVersion) < 0
        )
        {
            // 💡 query_stream is available since SurrealDB v3.3
            var response = await RawQuery(
                    query,
                    parameters,
                    sessionId,
                    transactionId,
                    SurrealDbWsRequestPriority.Normal,
                    cancellationToken
                )
                .ConfigureAwait(false);

            foreach (var frame in QueryStreamFallback.ToFrames(response))
            {
                yield return frame;
            }

            yield break;
        }

        // 💡 Unbounded on purpose: the server only slows down when the socket is not read,
        // which would stall every other request of this connection, and deadlock a consumer
        // that sends requests while it reads the stream
        var queryStream = Channel.CreateUnbounded<SurrealDbWsOkResponse>(
            new UnboundedChannelOptions { SingleReader = true }
        );

        var firstResponse = await SendRequestAsync(
                "query_stream",
                [query, parameters],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken,
                queryStream
            )
            .ConfigureAwait(false);

        string id = firstResponse.Id;
        bool isEnded = false;

        try
        {
            while (
                await queryStream.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)
            )
            {
                while (queryStream.Reader.TryRead(out var response))
                {
                    var frame =
                        response.GetValue<SurrealDbQueryStreamFrame>()
                        ?? throw new UnknownResponseTypeException();

                    isEnded = frame is SurrealDbQueryStreamEndFrame;

                    yield return frame;

                    if (isEnded)
                    {
                        yield break;
                    }
                }
            }
        }
        finally
        {
            if (isEnded)
            {
                _queryStreams.TryRemove(id, out _);
            }
            else if (_queryStreams.ContainsKey(id))
            {
                // 💡 Stops the query on the server when the stream is left early
                AbandonQueryStream(id, queryStream, sessionId, transactionId);
            }
        }
    }

    /// <summary>
    /// Streams left early within a transaction, by transaction id: they must end before the transaction does.
    /// </summary>
    private readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<string, Task>
    > _transactionStreams = new();

    /// <summary>
    /// Stops a stream that nobody reads any more.
    /// Outside a transaction, it is forgotten at once. Within a transaction, its frames are read until it ends,
    /// because committing or cancelling the transaction while the server still runs the stream would end
    /// the transaction under it.
    /// </summary>
    private void AbandonQueryStream(
        string id,
        Channel<SurrealDbWsOkResponse> queryStream,
        Guid? sessionId,
        Guid? transactionId
    )
    {
        if (!transactionId.HasValue)
        {
            _queryStreams.TryRemove(id, out _);
            _ = CancelQueryStreamAsync(id, sessionId);
            return;
        }

        var streams = _transactionStreams.GetOrAdd(transactionId.Value, _ => new());
        streams[id] = DrainQueryStreamAsync(id, queryStream, sessionId, transactionId.Value);
    }

    private async Task DrainQueryStreamAsync(
        string id,
        Channel<SurrealDbWsOkResponse> queryStream,
        Guid? sessionId,
        Guid transactionId
    )
    {
        try
        {
            // 💡 A refused cancellation means the server knows no such stream: it never started, or it has
            // already ended, and then its end frame is already queued, since responses arrive in order
            bool isRunning = await CancelQueryStreamAsync(id, sessionId).ConfigureAwait(false);

            while (true)
            {
                if (!queryStream.Reader.TryRead(out var response))
                {
                    if (!isRunning)
                    {
                        break;
                    }

                    if (!await queryStream.Reader.WaitToReadAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    continue;
                }

                if (response.GetValue<SurrealDbQueryStreamFrame>() is SurrealDbQueryStreamEndFrame)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // 💡 The connection is gone, and the transaction with it
        }
        finally
        {
            _queryStreams.TryRemove(id, out _);
            if (_transactionStreams.TryGetValue(transactionId, out var streams))
            {
                streams.TryRemove(id, out _);
            }
        }
    }

    /// <returns>Whether the server stopped a running stream.</returns>
    private async Task<bool> CancelQueryStreamAsync(string id, Guid? sessionId)
    {
        try
        {
            await SendRequestAsync(
                    "query_cancel",
                    [id],
                    sessionId,
                    null,
                    SurrealDbWsRequestPriority.Normal,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            // 💡 The query may have ended in the meantime, or the connection may be gone
            return false;
        }
    }

    /// <summary>
    /// Waits for the streams left early within a transaction to end.
    /// </summary>
    private async Task WaitForTransactionStreamsAsync(
        Guid transactionId,
        CancellationToken cancellationToken
    )
    {
        if (!_transactionStreams.TryRemove(transactionId, out var streams) || streams.IsEmpty)
        {
            return;
        }

        var allStreamsEnded = Task.WhenAll(streams.Values);
        var completedTask = await Task.WhenAny(
                allStreamsEnded,
                Task.Delay(Timeout.Infinite, cancellationToken)
            )
            .ConfigureAwait(false);
        if (completedTask != allStreamsEnded)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public async Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        TData? data,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [ins, table, outs, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var dbResponse = await SendRequestAsync(
                "relate",
                [@in, recordId, @out, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "run",
                [name, version, args],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Select<T>(
        string table,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [table],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>()!;
    }

    public async Task<T?> Select<T>(
        RecordId recordId,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [recordId],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<T?> Select<T>(
        StringRecordId recordId,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "select",
                [recordId],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        Guid? sessionId,
        Guid? transactionId,
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
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.Set(key, value);
    }

    public Task SignIn(
        RootAuth rootAuth,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        return SignIn(
            rootAuth,
            sessionId,
            transactionId,
            SurrealDbWsRequestPriority.Normal,
            cancellationToken
        );
    }

    private async Task SignIn(
        SystemAuth systemAuth,
        Guid? sessionId,
        Guid? transactionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync(
                "signin",
                [systemAuth],
                sessionId,
                transactionId,
                priority,
                cancellationToken
            )
            .ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.SetSystemAuth(systemAuth);
    }

    public async Task<Tokens> SignIn(
        NamespaceAuth nsAuth,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [nsAuth],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [dbAuth],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync(
                "signin",
                [scopeAuth],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : ScopeAuth
    {
        var dbResponse = await SendRequestAsync(
                "signup",
                [scopeAuth],
                sessionId,
                transactionId,
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
            throw new LiveQuerySurrealDbException("Live Query not found");
        }

        var liveQueryChannel = new SurrealDbLiveQueryChannel();
        _liveQueryChannelSubscriptions.Add(liveQueryChannel);

        return liveQueryChannel;
    }

    public async Task Unset(
        string key,
        Guid? sessionId,
        Guid? transactionId,
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
                "unset",
                [key],
                sessionId,
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.Unset(key);
    }

    public async Task<T> Update<T>(
        T data,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        if (_version is null || _version.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbMethodException("Cannot update a record without an Id");

        var dbResponse = await SendRequestAsync(
                "update",
                [data.Id, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var dbResponse = await SendRequestAsync(
                "update",
                [table, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        var dbResponse = await SendRequestAsync(
                "update",
                [table, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Upsert<T>(
        T data,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbMethodException("Cannot upsert a record without an Id");

        await EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        string method = _version?.Major > 1 ? "upsert" : "update";
        var dbResponse = await SendRequestAsync(
                method,
                [data.Id, data],
                sessionId,
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
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
        Guid? transactionId,
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
                transactionId,
                SurrealDbWsRequestPriority.Normal,
                cancellationToken
            )
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public Task Use(
        string ns,
        string db,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        return Use(
            ns,
            db,
            sessionId,
            transactionId,
            SurrealDbWsRequestPriority.Normal,
            cancellationToken
        );
    }

    private async Task Use(
        string ns,
        string db,
        Guid? sessionId,
        Guid? transactionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync(
                "use",
                [ns, db],
                sessionId,
                transactionId,
                priority,
                cancellationToken
            )
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
        var dbResponse = await SendRequestAsync(
                "version",
                null,
                null,
                null,
                priority,
                cancellationToken
            )
            .ConfigureAwait(false);
        string version = dbResponse.GetValue<string>()!;

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private CborOptions GetCborOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(_configureCborOptions);
    }

    /// <summary>
    /// Restores everything the server forgot when the previous connection closed:
    /// the root session state, every child session (attached again first), then the live queries.
    /// </summary>
    private async Task ApplyReconnectConfigurationAsync(CancellationToken cancellationToken)
    {
        var replayCompletion = _replayCompletion;

        try
        {
            string version = await Version(SurrealDbWsRequestPriority.High, cancellationToken)
                .ConfigureAwait(false);
            _version = version.ToSemver();

            await ApplyConfigurationAsync(null, null, cancellationToken).ConfigureAwait(false);

            foreach (var sessionId in SessionInfos.Enumerate())
            {
                if (!sessionId.HasValue)
                {
                    continue;
                }

                try
                {
                    await Attach(
                            sessionId.Value,
                            SurrealDbWsRequestPriority.High,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                    await ApplyConfigurationAsync(sessionId, null, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _surrealDbLoggerFactory?.Connection?.LogConnectionReplayFailed(
                        $"session {sessionId.Value}",
                        ex
                    );
                }
            }

            await ResubscribeLiveQueriesAsync(cancellationToken).ConfigureAwait(false);

            replayCompletion.TrySetResult(true);
        }
        catch (OperationCanceledException)
        {
            // 💡 Another reconnection took over, it will complete the replay
        }
        catch (Exception ex)
        {
            _surrealDbLoggerFactory?.Connection?.LogConnectionReplayFailed("root session", ex);

            // 💡 Let pending requests go through, they will report their own errors
            replayCompletion.TrySetResult(false);
        }
    }

    private async Task ResubscribeLiveQueriesAsync(CancellationToken cancellationToken)
    {
        foreach (var (queryUuid, definition) in _liveQueryDefinitions.ToArray())
        {
            try
            {
                var newServerId = definition.Table is not null
                    ? await StartLiveTable(
                            definition.Table,
                            definition.Diff,
                            definition.SessionId,
                            null,
                            SurrealDbWsRequestPriority.High,
                            cancellationToken
                        )
                        .ConfigureAwait(false)
                    : await StartLiveRawQuery(
                            definition.Query!,
                            definition.Parameters!,
                            definition.SessionId,
                            null,
                            SurrealDbWsRequestPriority.High,
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                _liveQueryIdsByServerId.TryRemove(definition.ServerId, out _);
                definition.ServerId = newServerId;
                _liveQueryIdsByServerId[newServerId] = queryUuid;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _surrealDbLoggerFactory?.Connection?.LogConnectionReplayFailed(
                    $"live query {queryUuid}",
                    ex
                );

                _liveQueryDefinitions.TryRemove(queryUuid, out _);
                await CloseLiveQueryChannelsAsync(
                        queryUuid,
                        SurrealDbLiveQueryClosureReason.SocketClosed
                    )
                    .ConfigureAwait(false);
            }
        }
    }

    private void LogReceivedMessage(System.IO.Stream stream)
    {
        if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
        {
            string cborData = CborDebugHelper.CborBinaryToHexa(stream);
            _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(cborData);
        }
    }

    private async Task<ISurrealDbWsResponse?> DeserializeMessageAsync(
        ResponseMessage message,
        CancellationToken cancellationToken
    )
    {
        if (message.MessageType != WebSocketMessageType.Binary)
        {
            return null;
        }

        using var stream =
            message.Stream ?? MemoryStreamProvider.MemoryStreamManager.GetStream(message.Binary!);

        LogReceivedMessage(stream);

        return await CborSerializer
            .DeserializeAsync<ISurrealDbWsResponse>(stream, GetCborOptions(), cancellationToken)
            .ConfigureAwait(false);
    }

    private ISurrealDbWsResponse? DeserializeMessage(ResponseMessage message)
    {
        if (message.MessageType != WebSocketMessageType.Binary)
        {
            return null;
        }

        if (message.Stream is null)
        {
            return CborSerializer.Deserialize<ISurrealDbWsResponse>(
                message.Binary!,
                GetCborOptions()
            );
        }

        using var stream = message.Stream;

        LogReceivedMessage(stream);

        stream.Position = 0;
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer))
        {
            return CborSerializer.Deserialize<ISurrealDbWsResponse>(
                buffer.AsSpan(0, (int)memoryStream.Length),
                GetCborOptions()
            );
        }

        using var copy = MemoryStreamProvider.MemoryStreamManager.GetStream();
        stream.CopyTo(copy);
        return CborSerializer.Deserialize<ISurrealDbWsResponse>(copy.ToArray(), GetCborOptions());
    }

    private void OnReceivedMessageProcessingFailed(Exception ex)
    {
        // 💡 A failure to process a single message (e.g. an unexpected CBOR payload)
        // must not terminate the underlying Rx subscription,
        // otherwise all subsequent responses would be silently dropped
        // and every pending request would end up in timeout.
#if DEBUG
        Debug.WriteLine(ex.Message);
#endif
        _surrealDbLoggerFactory?.Connection?.LogReceivedMessageProcessingFailed(ex);
    }

    /// <summary>
    /// Sends a response to the streaming query it belongs to, if any.
    /// </summary>
    private bool TryRouteQueryStreamResponse(ISurrealDbWsResponse? response)
    {
        if (
            response is not ISurrealDbWsStandardResponse standardResponse
            || !_queryStreams.TryGetValue(standardResponse.Id, out var channel)
        )
        {
            return false;
        }

        switch (response)
        {
            case SurrealDbWsOkResponse okResponse:
                channel.Writer.TryWrite(okResponse);
                break;
            case SurrealDbWsErrorResponse errorResponse:
                channel.Writer.TryComplete(errorResponse.Error.ToException());
                break;
        }

        // 💡 The first frame also completes the request that opened the stream
        CompleteResponseTask(response);
        return true;
    }

    private async Task HandleResponseAsync(ISurrealDbWsResponse? response)
    {
        try
        {
            if (response is SurrealDbWsLiveResponse surrealDbWsLiveResponse)
            {
                var liveQueryUuid = surrealDbWsLiveResponse.Result.Id;
                if (
                    _liveQueryIdsByServerId.TryGetValue(
                        liveQueryUuid,
                        out var originalLiveQueryUuid
                    )
                )
                {
                    liveQueryUuid = originalLiveQueryUuid;
                }

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

            CompleteResponseTask(response);
        }
        catch (Exception ex)
        {
            OnReceivedMessageProcessingFailed(ex);
        }
    }

    private void CompleteResponseTask(ISurrealDbWsResponse? response)
    {
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
                // 💡 The request may have been cancelled or timed out in the meantime
                case SurrealDbWsOkResponse okResponse:
                    responseTaskCompletionSource.TrySetResult(okResponse);
                    break;
                case SurrealDbWsErrorResponse errorResponse:
                    responseTaskCompletionSource.TrySetException(errorResponse.Error.ToException());
                    break;
                default:
                    responseTaskCompletionSource.TrySetException(
                        new UnknownResponseTypeException()
                    );
                    break;
            }
        }
    }

    private void OnDisconnection(DisconnectionInfo info)
    {
        if (_disposing)
        {
            // 💡 Live queries are closed by DisposeAsync
            return;
        }

        if (_replayCompletion.Task.IsCompleted)
        {
            _replayCompletion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
        }

        // 💡 Responses to requests sent on the lost connection will never arrive
        var connectionLostException = new SurrealDbConnectionException(
            "The connection was lost before a response was received.",
            "ConnectionLost",
            info.Exception
        );
#if NET9_0_OR_GREATER
        foreach (var (key, value) in _responseTasks)
        {
            if (_responseTasks.TryRemove(key, out _))
            {
                value.TrySetException(connectionLostException);
            }
        }
#else
        foreach (var (key, value) in _responseTaskHandler)
        {
            if (_responseTaskHandler.TryRemove(key, out _))
            {
                value.TrySetException(connectionLostException);
            }
        }
#endif

        // 💡 The rest of a streaming query will never arrive
        foreach (var (key, value) in _queryStreams)
        {
            if (_queryStreams.TryRemove(key, out _))
            {
                value.Writer.TryComplete(connectionLostException);
            }
        }

        // 💡 Live queries that cannot be started again are closed, the others wait for the reconnection
        foreach (var (key, value) in _liveQueryChannelSubscriptionsPerQuery)
        {
            if (value.WsEngineId == _id && !_liveQueryDefinitions.ContainsKey(key))
            {
                _ = CloseLiveQueryChannelsAsync(key, SurrealDbLiveQueryClosureReason.SocketClosed);
            }
        }
    }

    private async Task CloseLiveQueryChannelsAsync(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason
    )
    {
        if (
            !_liveQueryChannelSubscriptionsPerQuery.TryRemove(
                queryUuid,
                out var liveQueryChannelSubscriptions
            )
        )
        {
            return;
        }

        try
        {
            await Task.WhenAll(
                    liveQueryChannelSubscriptions.Select(liveQueryChannel =>
                        CloseLiveQueryAsync(liveQueryChannel, reason)
                    )
                )
                .ConfigureAwait(false);
        }
        catch
        {
            // 💡 Closing a channel must not stop the others from closing
        }
    }

    private static TaskCompletionSource<bool> CreateCompletedReplay()
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        completion.SetResult(true);
        return completion;
    }

    /// <summary>
    /// Drops the current connection and opens a new one, as a network failure would.
    /// </summary>
    internal Task ForceReconnectAsync()
    {
        return _wsClient.Reconnect();
    }

    private Task ApplyRootConfigurationAsync(CancellationToken cancellationToken)
    {
        return ApplyConfigurationAsync(null, null, cancellationToken);
    }

    private async Task ApplyConfigurationAsync(
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var session = SessionInfos.Get(sessionId)!;

        if (session.Ns is not null)
        {
            await Use(
                    session.Ns,
                    session.Db!,
                    sessionId,
                    transactionId,
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
                        transactionId,
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
                await SignIn(
                        nsAuth,
                        sessionId,
                        transactionId,
                        SurrealDbWsRequestPriority.High,
                        cancellationToken
                    )
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
                await SignIn(
                        dbAuth,
                        sessionId,
                        transactionId,
                        SurrealDbWsRequestPriority.High,
                        cancellationToken
                    )
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
                    transactionId,
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

        // 💡 Variables are replayed last, once the session is authenticated
        foreach (var (key, value) in session.Variables.ToArray())
        {
            await SendRequestAsync(
                    "let",
                    [key, value],
                    sessionId,
                    transactionId,
                    SurrealDbWsRequestPriority.High,
                    cancellationToken
                )
                .ConfigureAwait(false);
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
        Guid? transactionId,
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken,
        Channel<SurrealDbWsOkResponse>? queryStream = null
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        TimeSpan requestTimeout = _parameters.RequestTimeout ?? TimeSpan.FromSeconds(30);
        using var timeoutCts = new CancellationTokenSource(requestTimeout);
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

        if (priority == SurrealDbWsRequestPriority.Normal)
        {
            var replayTask = _replayCompletion.Task;
            if (!replayTask.IsCompleted)
            {
                await WaitForReplayAsync(replayTask, timeoutCts.Token, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (
            sessionId.HasValue
            && _sessionizer is not null
            && _sessionizer.Get(sessionId.Value, out var newSessionInfo)
            && newSessionInfo is RpcSessionInfo newRpcSessionInfo
        )
        {
            _sessionizer.TryRemove(sessionId.Value);
            // 💡 The provided state is shared by every scope, each session needs its own copy
            await CreateSession(
                    sessionId.Value,
                    new RpcSessionInfo(newRpcSessionInfo),
                    cancellationToken
                )
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

        if (queryStream is not null)
        {
            // 💡 Registered before sending, so that no frame is missed
            _queryStreams.TryAdd(id, queryStream);
        }

        bool isQueryStreamOpen = false;
        bool isRequestSent = false;

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
            TransactionId = transactionId,
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
            isRequestSent = true;

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

            isQueryStreamOpen = queryStream is not null;
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
            if (queryStream is not null && !isQueryStreamOpen)
            {
                // 💡 Given up before the server answered (cancellation, timeout),
                // the query may already run on the server and must be stopped there
                var openingTask = taskCompletionSource.Task;
                if (isRequestSent && !openingTask.IsCompletedSuccessfully && !openingTask.IsFaulted)
                {
                    AbandonQueryStream(id, queryStream, sessionId, transactionId);
                }
                else
                {
                    _queryStreams.TryRemove(id, out _);
                }
            }
        }
    }

    private static async Task WaitForReplayAsync(
        Task replayTask,
        CancellationToken timeoutToken,
        CancellationToken cancellationToken
    )
    {
        using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken);
        var delayTask = Task.Delay(Timeout.Infinite, delayCts.Token);
        var completedTask = await Task.WhenAny(replayTask, delayTask).ConfigureAwait(false);
        delayCts.Cancel();

        if (completedTask == replayTask)
        {
            if (replayTask.IsCanceled)
            {
                throw new EngineDisposedSurrealDbException();
            }

            return;
        }

        if (!cancellationToken.IsCancellationRequested)
            throw new TimeoutException();

        cancellationToken.ThrowIfCancellationRequested();
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
