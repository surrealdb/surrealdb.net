using Microsoft.Extensions.ObjectPool;

namespace SurrealDb.Net.Internals.ObjectPool;

internal class AsyncPooledObjectPolicy<T> : DefaultPooledObjectPolicy<T>
    where T : class, new()
{
    public Task<bool> ReturnAsync(T obj)
    {
        if (obj is IAsyncResettable resettable)
        {
            return resettable.TryResetAsync();
        }

        return Task.FromResult(true);
    }
}
