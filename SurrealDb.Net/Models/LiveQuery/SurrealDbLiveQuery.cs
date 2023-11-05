using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;
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
        await KillAsync().ConfigureAwait(false);
    }

    public async IAsyncEnumerator<SurrealDbLiveQueryResponse> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            await foreach (
                var response in surrealDbEngine
                    .GetLiveQueryChannel(Id)
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false)
            )
            {
                yield return ToSurrealDbLiveQueryResponse(response);
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
        if (_surrealDbEngine.TryGetTarget(out var surrealDbEngine))
        {
            await surrealDbEngine
                .Kill(Id, SurrealDbLiveQueryClosureReason.QueryKilled, cancellationToken)
                .ConfigureAwait(false);
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
            if (surrealDbWsLiveResponse.Result.Action == "CREATE")
            {
                return new SurrealDbLiveQueryCreateResponse<T>(
                    surrealDbWsLiveResponse.Result.GetValue<T>()!
                );
            }

            if (surrealDbWsLiveResponse.Result.Action == "UPDATE")
            {
                return new SurrealDbLiveQueryUpdateResponse<T>(
                    surrealDbWsLiveResponse.Result.GetValue<T>()!
                );
            }

            if (surrealDbWsLiveResponse.Result.Action == "DELETE")
            {
                return new SurrealDbLiveQueryDeleteResponse(
                    surrealDbWsLiveResponse.Result.GetValue<Thing>()!
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
