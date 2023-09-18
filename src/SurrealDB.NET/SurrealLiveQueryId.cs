namespace SurrealDB.NET;

public readonly record struct SurrealLiveQueryId : IAsyncDisposable
{
    public Guid Id { get; }

    internal SurrealLiveQueryId(Guid id, ISurrealRpcClient client)
    {
        Id = id;
		_client = new WeakReference<ISurrealRpcClient>(client);
    }

    public override string ToString()
    {
        return Id.ToString();
    }

	internal readonly WeakReference<ISurrealRpcClient> _client;

	public async ValueTask KillAsync(CancellationToken ct = default)
	{
		if (_client.TryGetTarget(out var client))
			await client.KillAsync(this, ct).ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync()
	{
		await KillAsync().ConfigureAwait(false);
	}
}
