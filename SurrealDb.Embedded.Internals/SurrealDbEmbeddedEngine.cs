using System.Diagnostics;
using System.Reactive;
using System.Runtime.InteropServices;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurrealDb.Embedded.Options;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Stream;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
#if  NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif

namespace SurrealDb.Embedded.Internals;

internal sealed partial class SurrealDbEmbeddedEngine : ISurrealDbProviderEngine
{
    private static int _globalId;

    private SurrealDbOptions? _parameters;
    private readonly SurrealDbEmbeddedOptions? _options;
    private Action<CborOptions>? _configureCborOptions;
    private ISurrealDbLoggerFactory? _surrealDbLoggerFactory;
    private ISessionizer? _sessionizer;

    private readonly int _id;

    private bool _isConnected;
    private bool _isInitialized;

#if DEBUG
    public string Id => _id.ToString();
#endif
    public Uri Uri { get; private set; } = new("unknown://");
    public EmbeddedSessionInfos SessionInfos { get; } = new();

    static SurrealDbEmbeddedEngine()
    {
        NativeMethods.create_global_runtime();
    }

    public SurrealDbEmbeddedEngine()
    {
        _id = Interlocked.Increment(ref _globalId);
    }

    public SurrealDbEmbeddedEngine(SurrealDbEmbeddedOptions? options)
        : this()
    {
        _options = options;
    }

    public SurrealDbEmbeddedEngine(IOptions<SurrealDbEmbeddedOptions> options)
        : this(options.Value) { }

    public void Initialize(
        SurrealDbOptions parameters,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory,
        ISessionizer? sessionizer
    )
    {
        Uri = new Uri(parameters.Endpoint!);
        _parameters = parameters;
        _configureCborOptions = configureCborOptions;
        _surrealDbLoggerFactory = surrealDbLoggerFactory;
        _sessionizer = sessionizer;

        // Set root session
        SessionInfos.Set(null, new EmbeddedSessionInfo(parameters));
    }

    public async Task Attach(Guid sessionId, CancellationToken cancellationToken)
    {
        await SendRequestAsync<Unit>(Method.Attach, null, sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task Authenticate(Tokens tokens, Guid? sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public async Task CloseSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await Detach(sessionId, cancellationToken).ConfigureAwait(false);

        SessionInfos.Remove(sessionId);
    }

    public async Task<Guid> CreateSession(CancellationToken cancellationToken)
    {
        var newId = Guid.NewGuid();

        await Attach(newId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(newId, new EmbeddedSessionInfo());

        return newId;
    }

    public async Task<Guid> CreateSession(Guid from, CancellationToken cancellationToken)
    {
        var newId = Guid.NewGuid();
        var newState = SessionInfos.Clone(from, newId);

        await Attach(newId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(newId, newState);

        return newId;
    }

    private async Task CreateSession(
        Guid sessionId,
        EmbeddedSessionInfo sessionInfo,
        CancellationToken cancellationToken
    )
    {
        await Attach(sessionId, cancellationToken).ConfigureAwait(false);
        SessionInfos.Set(sessionId, sessionInfo);
    }

    partial void PreConnect();

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (!_isConnected)
        {
            _surrealDbLoggerFactory?.Connection?.LogConnectionAttempt(_parameters!.Endpoint!);

            PreConnect();

            await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();

            await CborSerializer
                .SerializeAsync(_options, stream, GetCborOptions(), cancellationToken)
                .ConfigureAwait(false);

            if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
            {
                string cborData = CborDebugHelper.CborBinaryToHexa(stream);
                _surrealDbLoggerFactory?.Serialization?.LogSerializationDataSerialized(cborData);
            }

            bool canGetBuffer = stream.TryGetBuffer(out var bytes);
            if (!canGetBuffer)
            {
                throw new SurrealDbException("Failed to retrieve serialized buffer.");
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            Action<ByteBuffer> success = (_) =>
            {
                taskCompletionSource.SetResult(true);
            };
            Action<ByteBuffer> fail = (byteBuffer) =>
            {
                if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
                {
                    string cborData = CborDebugHelper.CborBinaryToHexa(byteBuffer.AsReadOnly());
                    _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(
                        cborData
                    );
                }

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
                        drop_callback = &NativeBindings.DropGcHandle,
                    },
                    callback = &NativeBindings.SuccessCallback,
                };

                var failureAction = new FailureAction()
                {
                    handle = new RustGCHandle()
                    {
                        ptr = GCHandle.ToIntPtr(failureHandle),
                        drop_callback = &NativeBindings.DropGcHandle,
                    },
                    callback = &NativeBindings.FailureCallback,
                };

                fixed (char* p = _parameters!.Endpoint)
                fixed (byte* payload = bytes.AsSpan())
                {
                    NativeMethods.apply_connect(
                        _id,
                        (ushort*)p,
                        _parameters.Endpoint!.Length,
                        payload,
                        bytes.Count,
                        successAction,
                        failureAction
                    );
                }
            }

            await taskCompletionSource.Task.ConfigureAwait(false);

            _isConnected = true;
        }

        await ApplyRootConfigurationAsync(cancellationToken).ConfigureAwait(false);

        _isInitialized = true;
    }

    public async Task<T> Create<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a record without an Id");

        return await SendRequestAsync<T>(
                Method.Create,
                [data.Id, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Create<T>(
        string table,
        T? data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(Method.Create, [table, data], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(
                Method.Create,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task Delete(string table, Guid? sessionId, CancellationToken cancellationToken)
    {
        await SendRequestAsync<Unit>(Method.Delete, [table], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(
        RecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var result = await SendRequestAsync<object?>(
                Method.Delete,
                [recordId],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
        return result is not null;
    }

    public async Task<bool> Delete(
        StringRecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        var result = await SendRequestAsync<object?>(
                Method.Delete,
                [recordId],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
        return result is not null;
    }

    public async Task Detach(Guid sessionId, CancellationToken cancellationToken)
    {
        await SendRequestAsync<Unit>(Method.Detach, null, sessionId, cancellationToken)
            .ConfigureAwait(false);
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

        if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
        {
            string cborData = CborDebugHelper.CborBinaryToHexa(stream);
            _surrealDbLoggerFactory?.Serialization?.LogSerializationDataSerialized(cborData);
        }

        bool canGetBuffer = stream.TryGetBuffer(out var bytes);
        if (!canGetBuffer)
        {
            throw new SurrealDbException("Failed to retrieve serialized buffer.");
        }

        var taskCompletionSource = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        Action<ByteBuffer> success = (byteBuffer) =>
        {
            if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
            {
                string cborData = CborDebugHelper.CborBinaryToHexa(byteBuffer.AsReadOnly());
                _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(cborData);
            }

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
            if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
            {
                string cborData = CborDebugHelper.CborBinaryToHexa(byteBuffer.AsReadOnly());
                _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(cborData);
            }

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
                    drop_callback = &NativeBindings.DropGcHandle,
                },
                callback = &NativeBindings.SuccessCallback,
            };

            var failureAction = new FailureAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(failureHandle),
                    drop_callback = &NativeBindings.DropGcHandle,
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
            await SendRequestAsync<Unit>(Method.Ping, null, null, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task Import(string input, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cancellationToken.Register(timeoutCts.Cancel);

        var taskCompletionSource = new TaskCompletionSource<Unit>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        Action<ByteBuffer> success = (_) =>
        {
            try
            {
                taskCompletionSource.SetResult(default);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        };
        Action<ByteBuffer> fail = (byteBuffer) =>
        {
            if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
            {
                string cborData = CborDebugHelper.CborBinaryToHexa(byteBuffer.AsReadOnly());
                _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(cborData);
            }

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
                    drop_callback = &NativeBindings.DropGcHandle,
                },
                callback = &NativeBindings.SuccessCallback,
            };

            var failureAction = new FailureAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(failureHandle),
                    drop_callback = &NativeBindings.DropGcHandle,
                },
                callback = &NativeBindings.FailureCallback,
            };

            fixed (char* p = input.AsSpan())
            {
                NativeMethods.import(_id, (ushort*)p, input.Length, successAction, failureAction);
            }
        }

        try
        {
            await taskCompletionSource.Task.ConfigureAwait(false);
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

    public Task<T> Info<T>(Guid? sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public async Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRecord
    {
        return await SendRequestAsync<List<T>>(
                Method.Insert,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> InsertRelation<T>(
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot create a relation record without an Id");

        var result = await SendRequestAsync<List<T>>(
                Method.InsertRelation,
                [null, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);

        return result.Single();
    }

    public async Task<T> InsertRelation<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord
    {
        if (data.Id is not null)
            throw new SurrealDbException(
                "You cannot provide both the table and an Id for the record. Either use the method overload without 'table' param or set the Id property to null."
            );

        var result = await SendRequestAsync<List<T>>(
                Method.InsertRelation,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);

        return result.Single();
    }

    public Task Invalidate(Guid? sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid, Guid? sessionId)
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        FormattableString query,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
    }

    public Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException();
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

        return await SendRequestAsync<TOutput>(
                Method.Merge,
                [data.Id, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(
                Method.Merge,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(
                Method.Merge,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TMerge : class
    {
        return await SendRequestAsync<IEnumerable<TOutput>>(
                Method.Merge,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Merge,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<T>(
                Method.Patch,
                [recordId, patches],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<T>(
                Method.Patch,
                [recordId, patches],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Patch,
                [table, patches],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        long executionStartTime = Stopwatch.GetTimestamp();

        var list = await SendRequestAsync<List<ISurrealDbResult>>(
                Method.Query,
                [query, parameters],
                sessionId,
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
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : class
    {
        return await SendRequestAsync<List<TOutput>>(
                Method.Relate,
                [ins, table, outs, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
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
        return await SendRequestAsync<TOutput>(
                Method.Relate,
                [@in, recordId, @out, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Run<T>(
        string name,
        string? version,
        object[]? args,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(
                Method.Run,
                [name, version, args],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Select<T>(
        string table,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Select,
                [table],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T?> Select<T>(
        RecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T?>(Method.Select, [recordId], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> Select<T>(
        StringRecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T?>(Method.Select, [recordId], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<IEnumerable<TOutput>>(
                Method.Select,
                [recordIdRange],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Guid>> Sessions(CancellationToken cancellationToken)
    {
        return await SendRequestAsync<IEnumerable<Guid>>(
                Method.Sessions,
                null,
                null,
                cancellationToken
            )
            .ConfigureAwait(false);
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

        await SendRequestAsync<Unit>(Method.Set, [key, value], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SignIn(RootAuth root, Guid? sessionId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Tokens> SignIn(
        NamespaceAuth nsAuth,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Tokens> SignIn(
        DatabaseAuth dbAuth,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Tokens> SignIn<T>(T scopeAuth, Guid? sessionId, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public Task<Tokens> SignUp<T>(T scopeAuth, Guid? sessionId, CancellationToken cancellationToken)
        where T : ScopeAuth
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id)
    {
        throw new NotSupportedException();
    }

    public Task<bool> TryResetAsync()
    {
        // 💡 No reuse needed when embedded
        return Task.FromResult(false);
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

        await SendRequestAsync<Unit>(Method.Unset, [key], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T> Update<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot update a record without an Id");

        return await SendRequestAsync<T>(
                Method.Update,
                [data.Id, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(
                Method.Update,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(
                Method.Update,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Update,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TOutput>> Update<TData, TOutput>(
        string table,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<IEnumerable<TOutput>>(
                Method.Update,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<T> Upsert<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord
    {
        if (data.Id is null)
            throw new SurrealDbException("Cannot upsert a record without an Id");

        return await SendRequestAsync<T>(
                Method.Upsert,
                [data.Id, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(
                Method.Upsert,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<IEnumerable<T>>(
                Method.Upsert,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<TOutput>> Upsert<TData, TOutput>(
        string table,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<IEnumerable<TOutput>>(
                Method.Upsert,
                [table, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(
                Method.Upsert,
                [recordId, data],
                sessionId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task Use(
        string ns,
        string db,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
    {
        await SendRequestAsync<Unit>(Method.Use, [ns, db], sessionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<string> Version(CancellationToken cancellationToken)
    {
        string version = await SendRequestAsync<string>(
                Method.Version,
                null,
                null,
                cancellationToken
            )
            .ConfigureAwait(false);

        const string VERSION_PREFIX = "surrealdb-";
        return version.Replace(VERSION_PREFIX, string.Empty);
    }

    private async Task ApplyRootConfigurationAsync(CancellationToken cancellationToken)
    {
        var session = SessionInfos.Get(null)!;

        if (session.Ns is not null)
        {
            await Use(session.Ns, session.Db!, null, cancellationToken).ConfigureAwait(false);

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
    }

    private CborOptions GetCborOptions()
    {
        return SurrealDbCborOptions.GetCborSerializerOptions(_configureCborOptions);
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
        Guid? sessionId,
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

        if (
            sessionId.HasValue
            && _sessionizer is not null
            && _sessionizer.Get(sessionId.Value, out var newSessionInfo)
            && newSessionInfo is EmbeddedSessionInfo newEmbeddedSessionInfo
        )
        {
            _sessionizer.TryRemove(sessionId.Value);
            await CreateSession(sessionId.Value, newEmbeddedSessionInfo, cancellationToken)
                .ConfigureAwait(false);
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

        if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
        {
            string cborData = CborDebugHelper.CborBinaryToHexa(stream);
            _surrealDbLoggerFactory?.Serialization?.LogSerializationDataSerialized(cborData);
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

        var taskCompletionSource = new TaskCompletionSource<T>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        timeoutCts.Token.Register(() =>
        {
            taskCompletionSource.TrySetCanceled();
        });

        bool expectOutput = typeof(T) != typeof(Unit);

        Action<ByteBuffer> success = (byteBuffer) =>
        {
            if (expectOutput)
            {
                if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
                {
                    string cborData = CborDebugHelper.CborBinaryToHexa(byteBuffer.AsReadOnly());
                    _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(
                        cborData
                    );
                }

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
            if (_surrealDbLoggerFactory?.Serialization?.IsEnabled(LogLevel.Debug) == true)
            {
                string cborData = CborDebugHelper.CborBinaryToHexa(byteBuffer.AsReadOnly());
                _surrealDbLoggerFactory?.Serialization?.LogSerializationDataDeserialized(cborData);
            }

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
                    drop_callback = &NativeBindings.DropGcHandle,
                },
                callback = &NativeBindings.SuccessCallback,
            };

            var failureAction = new FailureAction()
            {
                handle = new RustGCHandle()
                {
                    ptr = GCHandle.ToIntPtr(failureHandle),
                    drop_callback = &NativeBindings.DropGcHandle,
                },
                callback = &NativeBindings.FailureCallback,
            };

            var sessionBytes = sessionId.HasValue ? sessionId.Value.ToByteArray() : [];

            fixed (byte* session = sessionBytes.AsSpan())
            fixed (byte* payload = bytes.AsSpan())
            {
                NativeMethods.execute(
                    _id,
                    method,
                    session,
                    sessionBytes.Length,
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
}
