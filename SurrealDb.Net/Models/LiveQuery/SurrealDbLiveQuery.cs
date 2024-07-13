using System.Runtime.CompilerServices;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Models.LiveQuery;

public class SurrealDbLiveQuery<T> : IAsyncEnumerable<SurrealDbLiveQueryResponse>, IAsyncDisposable
{
    private readonly WeakReference<ISurrealDbEngine> _surrealDbEngine;

    public Guid Id { get; }

    internal SurrealDbLiveQuery(Guid id, ISurrealDbEngine surrealDbEngine)
    {
        Id = id;
        _surrealDbEngine = new WeakReference<ISurrealDbEngine>(surrealDbEngine);
    }

    public async ValueTask DisposeAsync()
    {
        await KillAsync(true).ConfigureAwait(false);
    }

    public async IAsyncEnumerator<SurrealDbLiveQueryResponse> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            var channel = surrealDbEngine.SubscribeToLiveQuery(Id);

            yield return new SurrealDbLiveQueryOpenResponse();

            await foreach (
                var response in channel.ReadAllAsync(cancellationToken).ConfigureAwait(false)
            )
            {
                yield return ToSurrealDbLiveQueryResponse(response);
            }

            yield break;
        }

        throw new Exception("SurrealDB instance has been disposed.");
    }

    /// <summary>
    /// Returns an enumerator that iterates asynchronously through the collection of results
    /// (all actions CREATE, UPDATE and DELETE, except OPEN and CLOSE).
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="Exception">When the SurrealDB client has been disposed.</exception>
    public async IAsyncEnumerable<SurrealDbLiveQueryResponse> GetResults(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            await foreach (
                var response in surrealDbEngine
                    .SubscribeToLiveQuery(Id)
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                if (response is SurrealDbWsClosedLiveResponse)
                    continue;

                yield return ToSurrealDbLiveQueryResponse(response);
            }

            yield break;
        }

        throw new Exception("SurrealDB instance has been disposed.");
    }

    /// <summary>
    /// Returns an enumerator that iterates asynchronously through the collection of created records.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="Exception">When the SurrealDB client has been disposed.</exception>
    public async IAsyncEnumerable<T> GetCreatedRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            await foreach (
                var response in surrealDbEngine
                    .SubscribeToLiveQuery(Id)
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                if (
                    response is SurrealDbWsLiveResponse surrealDbWsLiveResponse
                    && surrealDbWsLiveResponse.Result.Action == LiveQueryConstants.CREATE
                )
                {
                    yield return surrealDbWsLiveResponse.Result.GetValue<T>()!;
                }
            }

            yield break;
        }

        throw new Exception("SurrealDB instance has been disposed.");
    }

    /// <summary>
    /// Returns an enumerator that iterates asynchronously through the collection of updated records.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="Exception">When the SurrealDB client has been disposed.</exception>
    public async IAsyncEnumerable<T> GetUpdatedRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            await foreach (
                var response in surrealDbEngine
                    .SubscribeToLiveQuery(Id)
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                if (
                    response is SurrealDbWsLiveResponse surrealDbWsLiveResponse
                    && surrealDbWsLiveResponse.Result.Action == LiveQueryConstants.UPDATE
                )
                {
                    yield return surrealDbWsLiveResponse.Result.GetValue<T>()!;
                }
            }

            yield break;
        }

        throw new Exception("SurrealDB instance has been disposed.");
    }

    /// <summary>
    /// Returns an enumerator that iterates asynchronously through the collection of deleted records.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="Exception">When the SurrealDB client has been disposed.</exception>
    public async IAsyncEnumerable<T> GetDeletedRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            await foreach (
                var response in surrealDbEngine
                    .SubscribeToLiveQuery(Id)
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                if (
                    response is SurrealDbWsLiveResponse surrealDbWsLiveResponse
                    && surrealDbWsLiveResponse.Result.Action == LiveQueryConstants.DELETE
                )
                {
                    yield return surrealDbWsLiveResponse.Result.GetValue<T>()!;
                }
            }

            yield break;
        }

        throw new Exception("SurrealDB instance has been disposed.");
    }

    /// <summary>
    /// Kills the underlying live query.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Whether the Live Query was successfully killed or not.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    public async Task<bool> KillAsync(CancellationToken cancellationToken = default)
    {
        return await KillAsync(false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> KillAsync(
        bool onDispose,
        CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            var task = surrealDbEngine.Kill(
                Id,
                SurrealDbLiveQueryClosureReason.QueryKilled,
                cancellationToken
            );

            if (onDispose)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch { }
            }
            else
            {
                await task.ConfigureAwait(false);
            }

            return true;
        }

        return false;
    }

    private static SurrealDbLiveQueryResponse ToSurrealDbLiveQueryResponse(
        ISurrealDbWsLiveResponse response
    )
    {
        if (response is SurrealDbWsLiveResponse surrealDbWsLiveResponse)
        {
            if (surrealDbWsLiveResponse.Result.Action == LiveQueryConstants.CREATE)
            {
                return new SurrealDbLiveQueryCreateResponse<T>(
                    surrealDbWsLiveResponse.Result.GetValue<T>()!
                );
            }

            if (surrealDbWsLiveResponse.Result.Action == LiveQueryConstants.UPDATE)
            {
                return new SurrealDbLiveQueryUpdateResponse<T>(
                    surrealDbWsLiveResponse.Result.GetValue<T>()!
                );
            }

            if (surrealDbWsLiveResponse.Result.Action == LiveQueryConstants.DELETE)
            {
                return new SurrealDbLiveQueryDeleteResponse<T>(
                    surrealDbWsLiveResponse.Result.GetValue<T>()!
                );
            }
        }

        if (response is SurrealDbWsClosedLiveResponse surrealDbWsClosedLiveResponse)
        {
            return new SurrealDbLiveQueryCloseResponse(surrealDbWsClosedLiveResponse.Reason);
        }

        throw new SurrealDbException("Unknown action type for SurrealDB live query response.");
    }
}
