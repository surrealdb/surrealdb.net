namespace SurrealDb.Net.Internals.ObjectPool;

internal class SurrealDbClientPool : DefaultObjectPool<SurrealDbClientPoolContainer>, IDisposable
{
    private volatile bool _isDisposed;
    private protected AsyncPooledObjectPolicy<SurrealDbClientPoolContainer> _policy;

    public SurrealDbClientPool(AsyncPooledObjectPolicy<SurrealDbClientPoolContainer> policy)
        : base(policy)
    {
        _policy = policy;
    }

    public SurrealDbClientPool(
        AsyncPooledObjectPolicy<SurrealDbClientPoolContainer> policy,
        int maximumRetained
    )
        : base(policy, maximumRetained)
    {
        _policy = policy;
    }

    public override SurrealDbClientPoolContainer Get()
    {
        if (_isDisposed)
        {
            ThrowObjectDisposedException();
        }

        return base.Get();

        void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public override void Return(SurrealDbClientPoolContainer obj)
    {
        throw new NotImplementedException(
            "This method should not be called. Use ReturnAsync instead."
        );
    }

    public async Task ReturnAsync(SurrealDbClientPoolContainer obj)
    {
        if (_isDisposed || !(await ReturnAsyncCore(obj).ConfigureAwait(false)))
        {
            DisposeItem(obj);
        }
    }

    private async Task<bool> ReturnAsyncCore(SurrealDbClientPoolContainer obj)
    {
        if (!(await _policy.ReturnAsync(obj).ConfigureAwait(false)))
        {
            // policy says to drop this object
            return false;
        }

        if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
            {
                _items.Enqueue(obj);
                return true;
            }

            // no room, clean up the count and drop the object on the floor
            Interlocked.Decrement(ref _numItems);
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        _isDisposed = true;

        DisposeItem(_fastItem);
        _fastItem = null;

        while (_items.TryDequeue(out var item))
        {
            DisposeItem(item);
        }
    }

    private static void DisposeItem(SurrealDbClientPoolContainer? item)
    {
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
