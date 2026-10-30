using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Exceptions.Methods;
using SurrealDb.Net.Exceptions.Response;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Sessions;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif

namespace SurrealDb.Net.Internals;

internal class SurrealDbHttpEngine : ISurrealDbEngine
{
    private const string RPC_ENDPOINT = "/rpc";

    private const string SESSIONS_NOT_SUPPORTED_MESSAGE =
        "Sessions are not supported over HTTP. Use a WebSocket connection (ws:// or wss://) to create, fork or list sessions.";
    private const string TRANSACTIONS_NOT_SUPPORTED_MESSAGE =
        "Transactions are not supported over HTTP. Use a WebSocket connection (ws:// or wss://) to run interactive transactions.";

    internal SemVersion? _version { get; private set; }
    internal Action<CborOptions>? _configureCborOptions { get; }

    private int _pendingRequests;

#if DEBUG
    public string Id { get; } = Guid.NewGuid().ToString();
#endif

    private readonly SurrealDbOptions _parameters;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ISurrealDbLoggerFactory? _surrealDbLoggerFactory;
    private readonly ISessionizer? _sessionizer;

    public Uri Uri { get; }
    public RpcSessionInfos SessionInfos { get; } = new();

    public SurrealDbHttpEngine(
        SurrealDbOptions parameters,
        IHttpClientFactory? httpClientFactory,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory,
        ISessionizer? sessionizer
    )
    {
        Uri = new Uri(parameters.Endpoint!);
        _parameters = parameters;
        _httpClientFactory = httpClientFactory;
        _configureCborOptions = configureCborOptions;
        _surrealDbLoggerFactory = surrealDbLoggerFactory;
        _sessionizer = sessionizer;

        // Set root session
        SessionInfos.Set(null, new RpcSessionInfo(parameters));
    }

    public Task Attach(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(SESSIONS_NOT_SUPPORTED_MESSAGE);
    }

    public async Task Authenticate(
        Tokens tokens,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest
        {
            Method = "authenticate",
            Parameters = [tokens.Access],
        };

        await ExecuteRequestAsync(request, sessionId, cancellationToken).ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.SetBearerAuth(tokens.Access, tokens.Refresh);
    }

    public async Task<Tokens> Refresh(
        Tokens tokens,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "refresh", Parameters = [tokens] };

        // 💡 The access token may have expired already, it must not be sent as a header
        var dbResponse = await ExecuteRequestAsync(
                request,
                sessionId,
                cancellationToken,
                authOverride: new NoAuth()
            )
            .ConfigureAwait(false);
        var newTokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetBearerAuth(newTokens.Access, newTokens.Refresh);

        return newTokens;
    }

    public async Task Revoke(
        Tokens tokens,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "revoke", Parameters = [tokens] };

        await ExecuteRequestAsync(request, sessionId, cancellationToken).ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.ForgetRefreshToken(tokens.Refresh!);
    }

    public Task<Guid> Begin(Guid? sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(TRANSACTIONS_NOT_SUPPORTED_MESSAGE);
    }

    public Task Cancel(Guid? sessionId, Guid transactionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(TRANSACTIONS_NOT_SUPPORTED_MESSAGE);
    }

    /// <summary>
    /// Forgets the state of a session created by dependency injection (scoped or transient client).
    /// Such sessions only live on the client: each request carries their namespace, database and auth as headers.
    /// </summary>
    public Task CloseSession(Guid sessionId, CancellationToken cancellationToken)
    {
        SessionInfos.Remove(sessionId);
        return Task.CompletedTask;
    }

    public Task Commit(Guid? sessionId, Guid transactionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(TRANSACTIONS_NOT_SUPPORTED_MESSAGE);
    }

    public Task<Guid> CreateSession(CancellationToken cancellationToken)
    {
        throw new NotSupportedException(SESSIONS_NOT_SUPPORTED_MESSAGE);
    }

    public Task<Guid> CreateSession(Guid from, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(SESSIONS_NOT_SUPPORTED_MESSAGE);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters.Endpoint!);

        await Version(cancellationToken).ConfigureAwait(false);

        if (_version!.CompareSortOrderTo(new SemVersion(1, 4, 0)) < 0)
        {
            throw new SurrealDbConnectException(
                "CBOR is only supported on SurrealDB 1.4.0 or later."
            );
        }

        var dbResponse = await RawQuery(
                "RETURN TRUE",
                ImmutableDictionary<string, object?>.Empty,
                null,
                null,
                cancellationToken
            )
            .ConfigureAwait(false);
        EnsuresFirstResultOk(dbResponse);

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

        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [table] };

        await ExecuteRequestAsync(request, sessionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Delete(
        RecordId recordId,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public Task Detach(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(SESSIONS_NOT_SUPPORTED_MESSAGE);
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();

#if NET6_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return new ValueTask(Task.CompletedTask);
#endif
    }

    public async Task<bool> Health(CancellationToken cancellationToken)
    {
        using var wrapper = CreateEndpointHttpClientWrapper();
        using var body = CreateBodyContent(
            _configureCborOptions,
            new SurrealDbHttpRequest { Method = "ping" },
            null
        );
        using var httpRequest = CreateRpcRequestMessage(null, body);

        try
        {
            using var response = await wrapper
                .Instance.SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
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
        var request = new SurrealDbHttpRequest { Method = "info" };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "insert", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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

        var request = new SurrealDbHttpRequest
        {
            Method = "insert_relation",
            Parameters = [null, data],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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

        var request = new SurrealDbHttpRequest
        {
            Method = "insert_relation",
            Parameters = [table, data],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
    }

    public Task Invalidate(Guid? sessionId, Guid? transactionId, CancellationToken _)
    {
        SessionInfos.Get(sessionId)?.ResetAuth();
        return Task.CompletedTask;
    }

    public Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid, Guid? sessionId, Guid? transactionId)
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
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

        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "patch",
            Parameters = [recordId, patches],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "patch",
            Parameters = [recordId, patches],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "patch", Parameters = [table, patches] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var sessionParameters =
            SessionInfos.Get(sessionId)?.Variables ?? new Dictionary<string, object?>(0);

        var allParameters = new Dictionary<string, object?>(
            sessionParameters.Count + parameters.Count
        );

        foreach (var (key, value) in sessionParameters)
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
            Parameters = [query, allParameters],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
                allParameters,
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
        Guid? transactionId,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var request = new SurrealDbHttpRequest
        {
            Method = "relate",
            Parameters = [ins, table, outs, data],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "relate",
            Parameters = [@in, recordId, @out, data],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "run",
            Parameters = [name, version, args],
        };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [table] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T?> Select<T>(
        RecordId recordId,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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

        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [recordIdRange] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public Task<IEnumerable<Guid>> Sessions(CancellationToken cancellationToken)
    {
        throw new NotSupportedException(SESSIONS_NOT_SUPPORTED_MESSAGE);
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

        bool shouldEscapeKey = ShouldEscapeString(key);
        string escapedKey = shouldEscapeKey ? CreateEscaped(key) : key;

        var dbResponse = await RawQuery(
                $"RETURN ${escapedKey}",
                new Dictionary<string, object?>(capacity: 1) { { key, value } },
                sessionId,
                transactionId,
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsuresFirstResultOk(dbResponse);

        SessionInfos.Get(sessionId)?.Set(key, value);

        static bool ShouldEscapeString(string str)
        {
            if (long.TryParse(str, out _))
            {
                return true;
            }

            return !IsValidTextRecordId(str);
        }

        static bool IsValidTextRecordId(string str)
        {
            foreach (char c in str)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        static string CreateEscaped(string part)
        {
            return string.Create(
                part.Length + 2,
                part,
                (buffer, self) =>
                {
                    buffer.Write(RecordIdConstants.PREFIX);
                    buffer.Write(part);
                    buffer.Write(RecordIdConstants.SUFFIX);
                }
            );
        }
    }

    public async Task SignIn(
        RootAuth rootAuth,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [rootAuth] };

        await ExecuteRequestAsync(request, sessionId, cancellationToken).ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.SetSystemAuth(rootAuth);
    }

    public async Task<Tokens> SignIn(
        NamespaceAuth nsAuth,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [nsAuth] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [dbAuth] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [scopeAuth] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        var tokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetBearerAuth(tokens.Access, tokens.Refresh);

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
        var request = new SurrealDbHttpRequest { Method = "signup", Parameters = [scopeAuth] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        var tokens = dbResponse.GetValue<Tokens>()!;

        SessionInfos.Get(sessionId)?.SetBearerAuth(tokens.Access, tokens.Refresh);

        return tokens;
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        throw new NotSupportedException();
    }

    public Task<bool> TryResetAsync()
    {
        try
        {
            if (_pendingRequests > 0)
            {
                throw new SurrealDbResetException(
                    "Cannot reset client while requests are pending."
                );
            }

#if ENABLE_HTTP_RESET
            var request = new SurrealDbHttpRequest { Method = "reset" };
            await ExecuteRequestAsync(request, CancellationToken.None).ConfigureAwait(false);

            _config.Reset(_parameters);
            return true;
#else
            return Task.FromResult(false);
#endif
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public Task Unset(string key, Guid? sessionId, Guid? transactionId, CancellationToken _)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (!key.IsValidVariableName())
        {
            throw new ArgumentException("Variable name is not valid.", nameof(key));
        }

        SessionInfos.Get(sessionId)?.Unset(key);
        return Task.CompletedTask;
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

        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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

        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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

        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, sessionId, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task Use(
        string ns,
        string db,
        Guid? sessionId,
        Guid? transactionId,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "use", Parameters = [ns, db] };
        await ExecuteRequestAsync(request, sessionId, cancellationToken).ConfigureAwait(false);

        SessionInfos.Get(sessionId)?.Use(ns, db);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "version" };
        var dbResponse = await ExecuteRequestAsync(request, null, cancellationToken)
            .ConfigureAwait(false);
        var version = dbResponse.GetValue<string>()!;

        const string VERSION_PREFIX = "surrealdb-";
        version = version.Replace(VERSION_PREFIX, string.Empty);

        _version = version.ToSemver();

        return version;
    }

    private static CborOptions GetCborSerializerOptions(Action<CborOptions>? configureCborOptions)
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(configureCborOptions);
    }

    /// <summary>
    /// Creates an RPC request carrying the namespace, database and auth of the given session as headers.
    /// Headers are set on each request, never on the <see cref="HttpClient"/>, so that one client serves every session.
    /// </summary>
    private HttpRequestMessage CreateRpcRequestMessage(
        Guid? sessionId,
        HttpContent body,
        IAuth? authOverride = null
    )
    {
        var session = SessionInfos.Get(sessionId) ?? throw new InvalidSessionException(sessionId);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(Uri, RPC_ENDPOINT))
        {
            Content = body,
        };

        SetNsDbHttpClientHeaders(httpRequest.Headers, _version, session.Ns, session.Db);
        SetAuthHttpClientHeaders(httpRequest.Headers, authOverride ?? session.Auth);

        return httpRequest;
    }

    /// <summary>
    /// RPC methods that change the authentication themselves, so they never trigger a renewal.
    /// </summary>
    private static readonly HashSet<string> _methodsWithoutAuthRenewal =
    [
        "signin",
        "signup",
        "authenticate",
        "refresh",
        "revoke",
    ];

    private readonly SemaphoreSlim _authRenewalLock = new(1, 1);

    /// <summary>
    /// Renews the access token of a session when it is about to expire, using its refresh token,
    /// or forgets it once expired when <see cref="SurrealDbOptions.InvalidateOnExpiry"/> is set.
    /// A failed renewal is logged: the request then reports the expired token itself.
    /// </summary>
    internal async Task RenewAuthIfExpiringAsync(
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        if (!ShouldRenewAuth(sessionId))
        {
            return;
        }

        await _authRenewalLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // 💡 Another request may have renewed it while waiting
            if (
                !ShouldRenewAuth(sessionId)
                || SessionInfos.Get(sessionId)?.Auth is not BearerAuth bearerAuth
            )
            {
                return;
            }

            if (_parameters.InvalidateOnExpiry)
            {
                SessionInfos.Get(sessionId)?.ResetAuth();
                return;
            }

            await Refresh(
                    new Tokens(bearerAuth.Token, bearerAuth.Refresh),
                    sessionId,
                    null,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _surrealDbLoggerFactory?.Connection?.LogAuthRenewalFailed(ex);
        }
        finally
        {
            _authRenewalLock.Release();
        }
    }

    private bool ShouldRenewAuth(Guid? sessionId)
    {
        if (SessionInfos.Get(sessionId)?.Auth is not BearerAuth bearerAuth)
        {
            return false;
        }

        if (_parameters.InvalidateOnExpiry)
        {
            return JwtHelper.ExpiresWithin(bearerAuth.Token, TimeSpan.Zero);
        }

        return bearerAuth.Refresh is not null
            && JwtHelper.ExpiresWithin(bearerAuth.Token, _parameters.ExpiryMargin);
    }

    internal static void SetNsDbHttpClientHeaders(
        HttpRequestHeaders headers,
        SemVersion? version,
        string? ns,
        string? db
    )
    {
        headers.Remove(HttpConstants.ACCEPT_HEADER_NAME);

        if (version?.Major > 1)
        {
            headers.Remove(HttpConstants.NS_HEADER_NAME_V2);
            headers.Remove(HttpConstants.DB_HEADER_NAME_V2);
        }
        else
        {
            headers.Remove(HttpConstants.NS_HEADER_NAME);
            headers.Remove(HttpConstants.DB_HEADER_NAME);
        }

        headers.Add(HttpConstants.ACCEPT_HEADER_NAME, ["application/cbor"]);

        if (version?.Major > 1)
        {
            headers.Add(HttpConstants.NS_HEADER_NAME_V2, ns);
            headers.Add(HttpConstants.DB_HEADER_NAME_V2, db);
        }
        else
        {
            headers.Add(HttpConstants.NS_HEADER_NAME, ns);
            headers.Add(HttpConstants.DB_HEADER_NAME, db);
        }
    }

    internal static void SetAuthHttpClientHeaders(HttpRequestHeaders headers, IAuth? auth)
    {
        switch (auth)
        {
            case BearerAuth bearerAuth:
                headers.Authorization = new AuthenticationHeaderValue(
                    AuthConstants.BEARER,
                    bearerAuth.Token
                );
                break;
            case InternalSystemAuth systemAuth:
            {
                string credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(
                        $"{systemAuth.Auth.Username}:{systemAuth.Auth.Password}"
                    )
                );
                headers.Authorization = new AuthenticationHeaderValue(
                    AuthConstants.BASIC,
                    credentials
                );
                break;
            }
            case NoAuth:
                headers.Authorization = null;
                break;
        }
    }

    /// <summary>
    /// Returns the <see cref="HttpClient"/> to use for every HTTP call (RPC, import, export).
    /// Headers must be set on each request since the client is shared.
    /// </summary>
    internal HttpClientWrapper CreateEndpointHttpClientWrapper()
    {
        if (_httpClientFactory is not null)
        {
            string httpClientName = HttpClientHelper.GetHttpClientName(Uri);
            return new HttpClientWrapper(_httpClientFactory.CreateClient(httpClientName), true);
        }

        return new HttpClientWrapper(SharedHttpClient.Instance, false);
    }

    internal static HttpContent CreateBodyContent<T>(
        Action<CborOptions>? configureCborOptions,
        T data,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory
    )
    {
        var writer = new ArrayBufferWriter<byte>();
        CborSerializer.Serialize(data, writer, GetCborSerializerOptions(configureCborOptions));
        var payload = writer.WrittenSpan.ToArray();

        if (surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
        {
            string cborData = CborDebugHelper.CborBinaryToHexa(payload);
            surrealDbLoggerFactory?.Serialization?.LogSerializationDataSerialized(cborData);
        }

        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");

        return content;
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

    /// <param name="request">The RPC request.</param>
    /// <param name="sessionId">
    /// The client-side session whose namespace, database and auth are sent as headers.
    /// HTTP has no server-side session, so the id itself is never sent.
    /// </param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <param name="authOverride">The auth to send instead of the session's own, if any.</param>
    private async Task<SurrealDbHttpOkResponse> ExecuteRequestAsync(
        SurrealDbHttpRequest request,
        Guid? sessionId,
        CancellationToken cancellationToken,
        IAuth? authOverride = null
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        if (_version == null && request.Method != "version")
        {
            await Connect(cancellationToken).ConfigureAwait(false);
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
            SessionInfos.Set(sessionId.Value, new RpcSessionInfo(newRpcSessionInfo));
        }

        using var wrapper = CreateEndpointHttpClientWrapper();
        using var body = CreateBodyContent(_configureCborOptions, request, _surrealDbLoggerFactory);
        if (authOverride is null && !_methodsWithoutAuthRenewal.Contains(request.Method))
        {
            await RenewAuthIfExpiringAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }

        using var httpRequest = CreateRpcRequestMessage(sessionId, body, authOverride);

        Interlocked.Increment(ref _pendingRequests);

        SurrealDbHttpOkResponse surrealDbResponse;

        try
        {
            using var response = await wrapper
                .Instance.SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            surrealDbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _pendingRequests);
        }

#if NET7_0_OR_GREATER
        var executionTime = Stopwatch.GetElapsedTime(executionStartTime);
#else
        long executionEndTime = Stopwatch.GetTimestamp();
        var executionTime = TimeSpan.FromTicks(executionEndTime - executionStartTime);
#endif

        _surrealDbLoggerFactory?.Method?.LogMethodSuccess(
            request.Method,
            SurrealDbLoggerExtensions.FormatRequestParameters(
                request.Parameters!,
                _parameters.Logging.SensitiveDataLoggingEnabled
            ),
            SurrealDbLoggerExtensions.FormatExecutionTime(executionTime)
        );

        return surrealDbResponse;
    }

    private async Task<SurrealDbHttpOkResponse> DeserializeDbResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
#if NET6_0_OR_GREATER
        await using var stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var cborSerializerOptions = GetCborSerializerOptions(_configureCborOptions);

        if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
        {
            string cborData = CborDebugHelper.CborBinaryToHexa(stream);
            _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(cborData);
        }

        var result = await CborSerializer
            .DeserializeAsync<ISurrealDbHttpResponse>(
                stream,
                cborSerializerOptions,
                cancellationToken
            )
            .ConfigureAwait(false);

        return ExtractSurrealDbOkResponse(result);
    }

    private static SurrealDbHttpOkResponse ExtractSurrealDbOkResponse(
        ISurrealDbHttpResponse? result
    )
    {
        return result switch
        {
            SurrealDbHttpOkResponse okResponse => okResponse,
            SurrealDbHttpErrorResponse errorResponse => throw errorResponse.Error.ToException(),
            _ => throw new UnknownResponseTypeException(),
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
