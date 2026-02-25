using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net;

/// <summary>
/// The <see cref="SurrealDbTransaction"/> class provides transaction support for executing multiple queries atomically.
/// When all desired queries have been executed, call <see cref="Commit"/> to apply the changes to the database, or <see cref="Rollback"/> to discard them.
/// </summary>
public sealed class SurrealDbTransaction : IAsyncDisposable
{
    private readonly Guid? _sessionId;
    private readonly Guid _transactionId;
    private readonly WeakReference<ISurrealDbEngine> _engineReference;

#if NET9_0_OR_GREATER
    private readonly Lock _completeLock = new();
#else
    private readonly object _completeLock = new();
#endif
    private bool _isComplete;

    internal SurrealDbTransaction(
        Guid? sessionId,
        Guid transactionId,
        WeakReference<ISurrealDbEngine> engineReference
    )
    {
        _sessionId = sessionId;
        _transactionId = transactionId;
        _engineReference = engineReference;
    }

    /// <summary>
    /// Commit the transaction to the database, applying all changes made within the transaction scope.
    /// </summary>
    /// <remarks>
    /// After committing, the transaction cannot be used again.
    /// </remarks>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task Commit(CancellationToken cancellationToken = default)
    {
        bool canCommit = false;

        lock (_completeLock)
        {
            if (!_isComplete)
            {
                _isComplete = true;
                canCommit = true;
            }
        }

        if (!canCommit)
        {
            throw new SurrealDbTransactionException();
        }

        if (_engineReference.TryGetTarget(out var engine))
        {
            return engine.Commit(_sessionId, _transactionId, cancellationToken);
        }

        throw new EngineDisposedSurrealDbException();
    }

    /// <summary>
    /// Cancel and discard all changes made in the transaction.
    /// </summary>
    /// <remarks>
    /// After canceling, the transaction cannot be used again.
    /// </remarks>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task Rollback(CancellationToken cancellationToken = default)
    {
        return RollbackInternal(false, cancellationToken);
    }

    private Task RollbackInternal(bool disposed, CancellationToken cancellationToken)
    {
        bool shouldRollback = false;

        lock (_completeLock)
        {
            if (!_isComplete)
            {
                _isComplete = true;
                shouldRollback = true;
            }
        }

        if (!shouldRollback)
        {
            if (disposed)
                return Task.CompletedTask;

            throw new SurrealDbTransactionException();
        }

        if (_engineReference.TryGetTarget(out var engine))
        {
            return engine.Cancel(_sessionId, _transactionId, cancellationToken);
        }

        throw new EngineDisposedSurrealDbException();
    }

    public async ValueTask DisposeAsync()
    {
        await RollbackInternal(true, CancellationToken.None);
    }
}
