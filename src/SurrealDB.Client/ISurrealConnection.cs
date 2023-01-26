namespace SurrealDB.Client;

public interface ISurrealConnection
{
	Task<ISurrealClient> ConnectAsync(CancellationToken cancellationToken = default);
}
