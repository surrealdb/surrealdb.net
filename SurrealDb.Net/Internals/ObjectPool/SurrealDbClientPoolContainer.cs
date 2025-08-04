namespace SurrealDb.Net.Internals.ObjectPool;

internal sealed class SurrealDbClientPoolContainer : IDisposable, IAsyncResettable
{
    public ISurrealDbEngine? ClientEngine { get; set; }

    public Task<bool> TryResetAsync()
    {
        return ClientEngine is null ? Task.FromResult(false) : ClientEngine.TryResetAsync();
    }

    public void Dispose()
    {
        ClientEngine?.Dispose();
    }
}
