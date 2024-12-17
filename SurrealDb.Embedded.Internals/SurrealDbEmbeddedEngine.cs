using System.Diagnostics;
using System.Reactive;
using System.Runtime.InteropServices;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Stream;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Embedded.Internals;

internal sealed partial class SurrealDbEmbeddedEngine : ISurrealDbProviderEngine
{
    private static int _globalId;

    private SurrealDbOptions? _parameters;
    private Action<CborOptions>? _configureCborOptions;
    private ISurrealDbLoggerFactory? _surrealDbLoggerFactory;

    private readonly int _id;
    private readonly SurrealDbEmbeddedEngineConfig _config = new();

    private bool _isConnected;
    private bool _isInitialized;

#if DEBUG
    public string Id => _id.ToString();
#endif

    static SurrealDbEmbeddedEngine()
    {
        NativeMethods.create_global_runtime();
    }

    public SurrealDbEmbeddedEngine()
    {
        _id = Interlocked.Increment(ref _globalId);
    }

    public void Initialize(
        SurrealDbOptions parameters,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory
    )
    {
        _parameters = parameters;
        _configureCborOptions = configureCborOptions;
        _surrealDbLoggerFactory = surrealDbLoggerFactory;
    }

    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task Clear(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    partial void PreConnect();

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (!_isConnected)
        {
            _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters!.Endpoint!);

            PreConnect();

            var taskCompletionSource = new TaskCompletionSource<bool>();

            Action<ByteBuffer> success = (byteBuffer) =>
            {
                taskCompletionSource.SetResult(true);
            };
            Action<ByteBuffer> fail = (byteBuffer) =>
            {
                string error = CborSerializer.Deserialize<string>(
                    byteBuffer.AsReadOnly(),
                    GetCborOptions()
                );
                taskCompletionSource.SetException(new SurrealDbException(error));
            };

            var successHandle = GCHandle.Alloc(success);
            var failureHandle = GCHandle.Alloc(fail);

            unsafe
            {
                var successAction = new SuccessAction()
                {
                    handle = new RustGCHandle()
                    {
                        ptr = GCHandle.ToIntPtr(successHandle),
                        drop_callback = &NativeBindings.DropGcHandle
                    },
                    callback = &NativeBindings.SuccessCallback,
                };

                var failureAction = new FailureAction()
                {
                    handle = new RustGCHandle()
                    {
                        ptr = GCHandle.ToIntPtr(failureHandle),
                        drop_callback = &NativeBindings.DropGcHandle
                    },
                    callback = &NativeBindings.FailureCallback,
                };

                fixed (char* p = _parameters!.Endpoint)
                {
                    NativeMethods.apply_connect(
                        _id,
                        (ushort*)p,
                        _parameters.Endpoint!.Length,
                        successAction,
                        failureAction
                    );
                }
            }

            await taskCompletionSource.Task.ConfigureAwait(false);

            _isConnected = true;
        }

        if (_config.Ns is not null)
        {
            await Use(_config.Ns, _config.Db!, cancellationToken).ConfigureAwait(false);

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

        _isInitialized = true;
    }

    public async Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        return await SendRequestAsync<T>(Method.Create, [data.Id, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<T>(Method.Create, [table, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(Method.Create, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Delete(string table, CancellationToken cancellationToken)
    {
        await SendRequestAsync<Unit>(Method.Delete, [table], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync<object?>(Method.Delete, [recordId], cancellationToken)
            .ConfigureAwait(false);
        return result is not null;
    }

    public async Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync<object?>(Method.Delete, [recordId], cancellationToken)
            .ConfigureAwait(false);
        return result is not null;
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.dispose(_id);

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

    public async Task<string> Export(ExportOptions? options, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cancellationToken.Register(timeoutCts.Cancel);

        await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();

        try
        {
            await CborSerializer
                .SerializeAsync(options ?? new(), stream, GetCborOptions(), timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException();
            }

            throw;
        }

        bool canGetBuffer = stream.TryGetBuffer(out var bytes);
        if (!canGetBuffer)
        {
            throw new SurrealDbException("Failed to retrieve serialized buffer.");
        }

        var taskCompletionSource = new TaskCompletionSource<string>();
        timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        Action<ByteBuffer> success = (byteBuffer) =>
        {
            try
            {
                var result = CborSerializer.Deserialize<string>(
                    byteBuffer.AsReadOnly(),
                    GetCborOptions()
                );
                taskCompletionSource.SetResult(result!);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        };
        Action<ByteBuffer> fail = (byteBuffer) =>
        {
            string error = CborSerializer.Deserialize<string>(
                byteBuffer.AsReadOnly(),
                GetCborOptions()
            );
            taskCompletionSource.SetException(new SurrealDbException(error));
        };

        var successHandle = GCHandle.Alloc(success);
        var failureHandle = GCHandle.Alloc(fail);

        unsafe
        {
            var successAction = new SuccessAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(successHandle),
                    drop_callback = &NativeBindings.DropGcHandle
                },
                callback = &NativeBindings.SuccessCallback,
            };

            var failureAction = new FailureAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(failureHandle),
                    drop_callback = &NativeBindings.DropGcHandle
                },
                callback = &NativeBindings.FailureCallback,
            };

            fixed (byte* payload = bytes.AsSpan())
            {
                NativeMethods.export(_id, payload, bytes.Count, successAction, failureAction);
            }
        }

        try
        {
            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException();
            }

            throw;
        }
    }

    public async Task<bool> Health(CancellationToken cancellationToken)
    {
        try
        {
            await SendRequestAsync<Unit>(Method.Ping, null, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<T> Info<T>(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public async Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        return await SendRequestAsync<List<T>>(Method.Insert, [table, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken)
        where T : RelationRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a relation record without an Id");

        var result = await SendRequestAsync<List<T>>(
                Method.InsertRelation,
                [null, data],
                cancellationToken
            )
            .ConfigureAwait(false);

        return result.Single();
    }

    public async Task<T> InsertRelation<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : RelationRecord
    {
        if (data.Id is not null)
            throw new SurrealDbException(
                "You cannot provide both the table and an Id for the record. Either use the method overload without 'table' param or set the Id property to null."
            );

        var result = await SendRequestAsync<List<T>>(
                Method.InsertRelation,
                [table, data],
                cancellationToken
            )
            .ConfigureAwait(false);

        return result.Single();
    }

    public Task Invalidate(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
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
        where TMerge : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        return await SendRequestAsync<TOutput>(Method.Merge, [data.Id, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(Method.Merge, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(Method.Merge, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        return await SendRequestAsync<IEnumerable<TOutput>>(
                Method.Merge,
                [table, data],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Merge,
                [table, data],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<T>(Method.Patch, [recordId, patches], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<T>(Method.Patch, [recordId, patches], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Patch,
                [table, patches],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var list = await SendRequestAsync<List<ISurrealDbResult>>(
                Method.Query,
                [query, parameters],
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
                _parameters!.Logging.SensitiveDataLoggingEnabled
            ),
            SurrealDbLoggerExtensions.FormatExecutionTime(executionTime)
        );

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
        return await SendRequestAsync<List<TOutput>>(
                Method.Relate,
                [ins, table, outs, data],
                cancellationToken
            )
            .ConfigureAwait(false);
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
        return await SendRequestAsync<TOutput>(
                Method.Relate,
                [@in, recordId, @out, data],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Run<T>(
        string name,
        string? version,
        object[]? args,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(Method.Run, [name, version, args], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<IEnumerable<T>>(Method.Select, [table], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<T?>(Method.Select, [recordId], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<T?>(Method.Select, [recordId], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<IEnumerable<TOutput>>(
                Method.Select,
                [recordIdRange],
                cancellationToken
            )
            .ConfigureAwait(false);
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

        await SendRequestAsync<Unit>(Method.Set, [key, value], cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SignIn(RootAuth root, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        throw new NotSupportedException();
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

        await SendRequestAsync<Unit>(Method.Unset, [key], cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Update<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot update a record without an Id");

        return await SendRequestAsync<T>(Method.Update, [data.Id, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(Method.Update, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(Method.Update, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Update,
                [table, data],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot upsert a record without an Id");

        return await SendRequestAsync<T>(Method.Upsert, [data.Id, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(Method.Upsert, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Upsert,
                [table, data],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(Method.Upsert, [recordId, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        await SendRequestAsync<Unit>(Method.Use, [ns, db], cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        string version = await SendRequestAsync<string>(Method.Version, null, cancellationToken)
            .ConfigureAwait(false);

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private CborOptions GetCborOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(
            _parameters!.NamingPolicy,
            _configureCborOptions
        );
    }

    private readonly SemaphoreSlim _semaphoreConnect = new(1, 1);

    /// <summary>
    /// Prevent usage before initialized
    /// and ensures connection before sending a request.
    /// </summary>
    private async Task InternalConnectAsync(
        bool requireInitialized,
        CancellationToken cancellationToken
    )
    {
        if (!_isConnected)
        {
            await _semaphoreConnect.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await Connect(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphoreConnect.Release();
            }
        }

        if (requireInitialized && !_isInitialized)
        {
            await _semaphoreConnect.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await Connect(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphoreConnect.Release();
            }
        }
    }

    private async Task<T> SendRequestAsync<T>(
        Method method,
        object?[]? parameters,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cancellationToken.Register(timeoutCts.Cancel);

        bool requireInitialized = method != Method.Use;

        try
        {
            await InternalConnectAsync(requireInitialized, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _surrealDbLoggerFactory?.Method?.LogMethodFailed(method.ToString(), "Timeout"); // TODO : Avoid ToString()
                throw new TimeoutException();
            }

            throw;
        }

        await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();

        try
        {
            await CborSerializer
                .SerializeAsync(parameters ?? [], stream, GetCborOptions(), timeoutCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _surrealDbLoggerFactory?.Method?.LogMethodFailed(method.ToString(), "Timeout"); // TODO : Avoid ToString()
                throw new TimeoutException();
            }

            throw;
        }

        bool canGetBuffer = stream.TryGetBuffer(out var bytes);
        if (!canGetBuffer)
        {
            _surrealDbLoggerFactory?.Method?.LogMethodFailed(
                method.ToString(),
                "Failed to retrieve serialized buffer."
            ); // TODO : Avoid ToString()
            throw new SurrealDbException("Failed to retrieve serialized buffer.");
        }

        var taskCompletionSource = new TaskCompletionSource<T>();
        timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        bool expectOutput = typeof(T) != typeof(Unit);

        Action<ByteBuffer> success = (byteBuffer) =>
        {
            if (expectOutput)
            {
                try
                {
                    var result = CborSerializer.Deserialize<T>(
                        byteBuffer.AsReadOnly(),
                        GetCborOptions()
                    );
                    taskCompletionSource.SetResult(result!);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }
            else
            {
                taskCompletionSource.SetResult(default!);
            }
        };
        Action<ByteBuffer> fail = (byteBuffer) =>
        {
            string error = CborSerializer.Deserialize<string>(
                byteBuffer.AsReadOnly(),
                GetCborOptions()
            );
            taskCompletionSource.SetException(new SurrealDbException(error));
        };

        var successHandle = GCHandle.Alloc(success);
        var failureHandle = GCHandle.Alloc(fail);

        unsafe
        {
            var successAction = new SuccessAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(successHandle),
                    drop_callback = &NativeBindings.DropGcHandle
                },
                callback = &NativeBindings.SuccessCallback,
            };

            var failureAction = new FailureAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(failureHandle),
                    drop_callback = &NativeBindings.DropGcHandle
                },
                callback = &NativeBindings.FailureCallback,
            };

            fixed (byte* payload = bytes.AsSpan())
            {
                NativeMethods.execute(
                    _id,
                    method,
                    payload,
                    bytes.Count,
                    successAction,
                    failureAction
                );
            }
        }

        try
        {
#if NET7_0_OR_GREATER
            var executionTime = Stopwatch.GetElapsedTime(executionStartTime);
#else
            long executionEndTime = Stopwatch.GetTimestamp();
            var executionTime = TimeSpan.FromTicks(executionEndTime - executionStartTime);
#endif

            _surrealDbLoggerFactory?.Method?.LogMethodSuccess(
                method.ToString(), // TODO : Avoid ToString()
                SurrealDbLoggerExtensions.FormatRequestParameters(
                    parameters!,
                    _parameters!.Logging.SensitiveDataLoggingEnabled
                ),
                SurrealDbLoggerExtensions.FormatExecutionTime(executionTime)
            );

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _surrealDbLoggerFactory?.Method?.LogMethodFailed(method.ToString(), "Timeout"); // TODO : Avoid ToString()
                throw new TimeoutException();
            }

            throw;
        }
    }

    public Task<bool> TryResetAsync()
    {
        return Task.FromResult(true);
    }
}
