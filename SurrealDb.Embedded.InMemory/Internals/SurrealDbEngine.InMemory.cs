using System.Net;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Dahomey.Cbor;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Internals.Stream;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Embedded.InMemory.Internals;

internal class SurrealDbInMemoryEngine : ISurrealDbInMemoryEngine
{
    private static int _globalId;

    private SurrealDbClientParams? _parameters;
    private Action<CborOptions>? _configureCborOptions;
    private readonly int _id;
    private readonly SurrealDbEmbeddedEngineConfig _config = new();

    private bool _isConnected;
    private bool _isInitialized;

    static SurrealDbInMemoryEngine()
    {
        NativeMethods.create_global_runtime();
    }

    public SurrealDbInMemoryEngine()
    {
        _id = Interlocked.Increment(ref _globalId);
    }

    public void Initialize(
        SurrealDbClientParams parameters,
        Action<CborOptions>? configureCborOptions
    )
    {
        _parameters = parameters;
        _configureCborOptions = configureCborOptions;

        if (_parameters.Serialization?.ToLowerInvariant() == SerializationConstants.JSON)
        {
            throw new NotSupportedException(
                "The JSON serialization is not supported for the in-memory provider."
            );
        }
    }

    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Authentication is not enabled in embedded mode.");
    }

    public void Configure(string? ns, string? db, string? username, string? password)
    {
        // 💡 Pre-configuration before connect
        if (ns is not null)
            _config.Use(ns, db);
    }

    public void Configure(string? ns, string? db, string? token = null)
    {
        // 💡 Pre-configuration before connect
        if (ns is not null)
            _config.Use(ns, db);
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (!_isConnected)
        {
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

                NativeMethods.apply_connect(_id, successAction, failureAction);
            }

            await taskCompletionSource.Task.ConfigureAwait(false);

            _isConnected = true;
        }

        if (_config.Ns is not null)
        {
            await Use(_config.Ns, _config.Db!, cancellationToken).ConfigureAwait(false);
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
        var list = await SendRequestAsync<IEnumerable<T>>(
                Method.Create,
                [table, data],
                cancellationToken
            )
            .ConfigureAwait(false);

        return list.First();
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

    public async Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync<object?>(Method.Delete, [thing], cancellationToken)
            .ConfigureAwait(false);
        return result is not null;
    }

    public async Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync<object?>(Method.Delete, [recordId], cancellationToken)
            .ConfigureAwait(false);
        return result is not null;
    }

    public void Dispose()
    {
        NativeMethods.dispose(_id);
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
        Thing thing,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        return await SendRequestAsync<T>(Method.Merge, [thing, data], cancellationToken)
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

    public async Task<IEnumerable<TOutput>> MergeAll<TMerge, TOutput>(
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

    public async Task<IEnumerable<T>> MergeAll<T>(
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
        Thing thing,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await SendRequestAsync<T>(Method.Patch, [thing, patches], cancellationToken)
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

    public async Task<IEnumerable<T>> PatchAll<T>(
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

    public async Task<SurrealDbResponse> Query(
        FormattableString query,
        CancellationToken cancellationToken,
        bool logIt = false
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return await RawQuery(formattedQuery, parameters, cancellationToken, logIt)
            .ConfigureAwait(false);
    }

    public async Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken,
        bool logIt = false
    )
    {
        var list = await SendRequestAsync<List<ISurrealDbResult>>(
                Method.Query,
                [query, parameters],
                cancellationToken
            )
            .ConfigureAwait(false);
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
        return await SendRequestAsync<List<TOutput>>(
                Method.Relate,
                [ins, table, outs, data],
                cancellationToken
            )
            .ConfigureAwait(false);
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
        return await SendRequestAsync<TOutput>(
                Method.Relate,
                [@in, thing, @out, data],
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<IEnumerable<T>>(Method.Select, [table], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<T?>(Method.Select, [thing], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<T?>(Method.Select, [recordId], cancellationToken)
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

    public async Task<IEnumerable<T>> UpdateAll<T>(
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
            throw new SurrealDbException("Cannot create a record without an Id");

        return await SendRequestAsync<T>(Method.Update, [data.Id, data], cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord
    {
        return await SendRequestAsync<TOutput>(Method.Update, [recordId, data], cancellationToken)
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
        bool requireInitialized = method != Method.Use;
        await InternalConnectAsync(requireInitialized, cancellationToken).ConfigureAwait(false);

        await using var stream = MemoryStreamProvider.MemoryStreamManager.GetStream();
        await CborSerializer
            .SerializeAsync(parameters ?? [], stream, GetCborOptions(), cancellationToken)
            .ConfigureAwait(false);

        bool canGetBuffer = stream.TryGetBuffer(out var bytes);
        if (!canGetBuffer)
        {
            throw new SurrealDbException("Failed to retrieve serialized buffer.");
        }

        var taskCompletionSource = new TaskCompletionSource<T>();

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

        return await taskCompletionSource.Task.ConfigureAwait(false);
    }
}
