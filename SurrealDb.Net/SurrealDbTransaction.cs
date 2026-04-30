using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net;

/// <summary>
/// The <see cref="SurrealDbTransaction"/> class provides transaction support for executing multiple queries atomically.
/// When all desired queries have been executed, call <see cref="Commit"/> to apply the changes to the database, or <see cref="Cancel"/> to discard them.
/// </summary>
public sealed class SurrealDbTransaction : SurrealDbSession
{
#if NET9_0_OR_GREATER
    private readonly Lock _completeLock = new();
#else
    private readonly object _completeLock = new();
#endif
    private bool _isComplete;

    internal SurrealDbTransaction(BaseSurrealDbClient from, Guid sessionId, Guid transactionId)
        : base(from, sessionId, transactionId) { }

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

        return Engine.Commit(SessionId, TransactionId!.Value, cancellationToken);
    }

    /// <summary>
    /// Cancel and discard all changes made in the transaction.
    /// </summary>
    /// <remarks>
    /// After canceling, the transaction cannot be used again.
    /// </remarks>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task Cancel(CancellationToken cancellationToken = default)
    {
        return CancelInternal(false, cancellationToken);
    }

    private Task CancelInternal(bool disposed, CancellationToken cancellationToken)
    {
        bool shouldCancel = false;

        lock (_completeLock)
        {
            if (!_isComplete)
            {
                _isComplete = true;
                shouldCancel = true;
            }
        }

        if (!shouldCancel)
        {
            if (disposed)
                return Task.CompletedTask;

            throw new SurrealDbTransactionException();
        }

        return Engine.Cancel(SessionId, TransactionId!.Value, cancellationToken);
    }

    public new async ValueTask DisposeAsync()
    {
        await CancelInternal(true, CancellationToken.None);
    }
}
