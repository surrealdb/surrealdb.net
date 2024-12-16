using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Internals;

internal class SurrealDbHttpEngine : ISurrealDbEngine
{
    private const string RPC_ENDPOINT = "/rpc";

    internal SemVersion? _version { get; private set; }
    internal Action<CborOptions>? _configureCborOptions { get; }
    internal SurrealDbHttpEngineConfig _config { get; }

    private readonly Uri _uri;
    private readonly SurrealDbOptions _parameters;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ISurrealDbLoggerFactory? _surrealDbLoggerFactory;
    private readonly Lazy<HttpClient> _singleHttpClient = new(() => new HttpClient(), true);
    private HttpClientConfiguration? _singleHttpClientConfiguration;

    private int _pendingRequests;

#if DEBUG
    private string _id = Guid.NewGuid().ToString();
    public string Id => _id;
#endif

    public SurrealDbHttpEngine(
        SurrealDbOptions parameters,
        IHttpClientFactory? httpClientFactory,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory
    )
    {
        _uri = new Uri(parameters.Endpoint!);
        _parameters = parameters;
        _httpClientFactory = httpClientFactory;
        _configureCborOptions = configureCborOptions;
        _surrealDbLoggerFactory = surrealDbLoggerFactory;
        _config = new(_parameters);
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

    public async Task Clear(CancellationToken cancellationToken)
    {
        if (_pendingRequests > 0)
        {
            throw new SurrealDbException("Cannot clear client while requests are pending.");
        }

        var request = new SurrealDbHttpRequest { Method = "clear" };
        await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters.Endpoint!);

        string version = await Version(cancellationToken).ConfigureAwait(false);
        _version = version.ToSemver();

        if (_version.CompareSortOrderTo(new SemVersion(1, 4, 0)) < 0)
        {
            throw new SurrealDbException("CBOR is only supported on SurrealDB 1.4.0 or later.");
        }

        var dbResponse = await RawQuery(
                "RETURN TRUE",
                ImmutableDictionary<string, object?>.Empty,
                cancellationToken
            )
            .ConfigureAwait(false);
        EnsuresFirstResultOk(dbResponse);

        _surrealDbLoggerFactory?.Connection?.LogConnectionSuccess(_parameters.Endpoint!);
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "create", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [table] };

        await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return !dbResponse.ExpectNone() && !dbResponse.ExpectEmptyArray();
    }

    public async Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "delete", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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

        if (_singleHttpClient.IsValueCreated)
        {
            _singleHttpClient.Value.Dispose();
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
        using var wrapper = CreateHttpClientWrapper();
        using var body = CreateBodyContent(
            _parameters.NamingPolicy,
            _configureCborOptions,
            new SurrealDbHttpRequest { Method = "ping" }
        );

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

    public async Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        var request = new SurrealDbHttpRequest { Method = "insert", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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

        var request = new SurrealDbHttpRequest
        {
            Method = "insert_relation",
            Parameters = [null, data]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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

        var request = new SurrealDbHttpRequest
        {
            Method = "insert_relation",
            Parameters = [table, data]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);

        return dbResponse.DeserializeEnumerable<T>().Single();
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
        where TMerge : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "merge", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
    }

    public async Task<IEnumerable<T>> Merge<T>(
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
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var request = new SurrealDbHttpRequest
        {
            Method = "patch",
            Parameters = [recordId, patches]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "patch",
            Parameters = [recordId, patches]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "patch", Parameters = [table, patches] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        var request = new SurrealDbHttpRequest
        {
            Method = "relate",
            Parameters = [ins, table, outs, data]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "relate",
            Parameters = [@in, recordId, @out, data]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest
        {
            Method = "run",
            Parameters = [name, version, args]
        };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T>()!;
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [table] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<T>();
    }

    public async Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<T?>();
    }

    public async Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [recordId] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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

        var request = new SurrealDbHttpRequest { Method = "select", Parameters = [recordIdRange] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.DeserializeEnumerable<TOutput>();
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

        bool shouldEscapeKey = ShouldEscapeString(key);
        string escapedKey = shouldEscapeKey ? CreateEscaped(key) : key;

        var dbResponse = await RawQuery(
                $"RETURN ${escapedKey}",
                new Dictionary<string, object?>(capacity: 1) { { key, value } },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsuresFirstResultOk(dbResponse);

        _config.SetParam(key, value);

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

        return new Jwt(token!);
    }

    public async Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [dbAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
    }

    public async Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var request = new SurrealDbHttpRequest { Method = "signin", Parameters = [scopeAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
    }

    public async Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        var request = new SurrealDbHttpRequest { Method = "signup", Parameters = [scopeAuth] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var token = dbResponse.GetValue<string>();

        _config.SetBearerAuth(token!);

        return new Jwt(token!);
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        throw new NotSupportedException();
    }

    public async Task<bool> TryResetAsync()
    {
        try
        {
            await Clear(default).ConfigureAwait(false);
            _config.Reset(_parameters);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
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

    public async Task<T> Update<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (_version?.Major < 2)
            throw new NotImplementedException();

        if (data.Id is null)
            throw new SurrealDbException("Cannot update a record without an Id");

        string method = _version?.Major > 1 ? "upsert" : "";
        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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

        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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

        var request = new SurrealDbHttpRequest { Method = "update", Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot upsert a record without an Id");

        string method = _version?.Major > 1 ? "upsert" : "update";
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [data.Id, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [table, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
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
        var request = new SurrealDbHttpRequest { Method = method, Parameters = [recordId, data] };

        var dbResponse = await ExecuteRequestAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return dbResponse.GetValue<TOutput>()!;
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
        var version = dbResponse.GetValue<string>()!;

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private static CborOptions GetCborSerializerOptions(
        string? namingPolicy,
        Action<CborOptions>? configureCborOptions
    )
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(namingPolicy, configureCborOptions);
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
            if (_version is not null && TrySetSingleHttpClientConfiguration(ns, db, _config.Auth))
            {
                ApplyHttpClientConfiguration(client, overridedAuth, useConfiguration);
                return client;
            }

            var desiredClientConfiguration = new HttpClientConfiguration(
                ns,
                db,
                overridedAuth ?? _config.Auth
            );
            bool shouldClone =
                _version is null || _singleHttpClientConfiguration != desiredClientConfiguration;

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

        var ns = useConfiguration is not null ? useConfiguration.Ns : _config.Ns;
        var db = useConfiguration is not null ? useConfiguration.Db : _config.Db;
        SetNsDbHttpClientHeaders(client, _version, ns, db);

        var auth = overridedAuth ?? _config.Auth;
        SetAuthHttpClientHeaders(client, auth);
    }

    internal static void SetNsDbHttpClientHeaders(
        HttpClient client,
        SemVersion? version,
        string? ns,
        string? db
    )
    {
        client.DefaultRequestHeaders.Remove(HttpConstants.ACCEPT_HEADER_NAME);

        if (version?.Major > 1)
        {
            client.DefaultRequestHeaders.Remove(HttpConstants.NS_HEADER_NAME_V2);
            client.DefaultRequestHeaders.Remove(HttpConstants.DB_HEADER_NAME_V2);
        }
        else
        {
            client.DefaultRequestHeaders.Remove(HttpConstants.NS_HEADER_NAME);
            client.DefaultRequestHeaders.Remove(HttpConstants.DB_HEADER_NAME);
        }

        client.DefaultRequestHeaders.Add(HttpConstants.ACCEPT_HEADER_NAME, ["application/cbor"]);

        if (version?.Major > 1)
        {
            client.DefaultRequestHeaders.Add(HttpConstants.NS_HEADER_NAME_V2, ns);
            client.DefaultRequestHeaders.Add(HttpConstants.DB_HEADER_NAME_V2, db);
        }
        else
        {
            client.DefaultRequestHeaders.Add(HttpConstants.NS_HEADER_NAME, ns);
            client.DefaultRequestHeaders.Add(HttpConstants.DB_HEADER_NAME, db);
        }
    }

    internal static void SetAuthHttpClientHeaders(HttpClient client, IAuth? auth)
    {
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

#if NET9_0_OR_GREATER
    private readonly Lock _singleHttpClientConfigurationLock = new();
#else
    private readonly object _singleHttpClientConfigurationLock = new();
#endif

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

    internal static HttpContent CreateBodyContent<T>(
        string? namingPolicy,
        Action<CborOptions>? configureCborOptions,
        T data
    )
    {
        var writer = new ArrayBufferWriter<byte>();
        CborSerializer.Serialize(
            data,
            writer,
            GetCborSerializerOptions(namingPolicy, configureCborOptions)
        );
        var payload = writer.WrittenSpan.ToArray();

        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");

        return content;
    }

    private async Task<SurrealDbHttpOkResponse> ExecuteRequestAsync(
        SurrealDbHttpRequest request,
        CancellationToken cancellationToken
    )
    {
        Interlocked.Increment(ref _pendingRequests);

        try
        {
            long executionStartTime = Stopwatch.GetTimestamp();

            if (_version == null && request.Method != "version")
            {
                await Connect(cancellationToken).ConfigureAwait(false);
            }

            using var wrapper = CreateHttpClientWrapper();
            using var body = CreateBodyContent(
                _parameters.NamingPolicy,
                _configureCborOptions,
                request
            );

            using var response = await wrapper
                .Instance.PostAsync(RPC_ENDPOINT, body, cancellationToken)
                .ConfigureAwait(false);

            var surrealDbResponse = await DeserializeDbResponseAsync(response, cancellationToken)
                .ConfigureAwait(false);

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
        finally
        {
            Interlocked.Decrement(ref _pendingRequests);
        }
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
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        var cborSerializerOptions = GetCborSerializerOptions(
            _parameters.NamingPolicy,
            _configureCborOptions
        );

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
