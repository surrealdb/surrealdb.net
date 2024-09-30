namespace SurrealDb.Net.Internals.ObjectPool;

internal class SurrealDbClientPoolContainer : IDisposable, IAsyncResettable
{
    public ISurrealDbEngine? ClientEngine { get; set; }

    public Task<bool> TryResetAsync()
    {
        if (ClientEngine is null)
        {
            return Task.FromResult(false);
        }

        return ClientEngine.TryResetAsync();
    }

    public void Dispose()
    {
        ClientEngine?.Dispose();
    }
}
