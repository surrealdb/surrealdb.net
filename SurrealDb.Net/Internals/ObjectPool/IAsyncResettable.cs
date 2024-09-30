namespace SurrealDb.Net.Internals.ObjectPool;

public interface IAsyncResettable
{
    Task<bool> TryResetAsync();
}
