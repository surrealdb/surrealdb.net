namespace SurrealDb.Internals.Ws;

internal class SurrealWsTaskCompletionSource : TaskCompletionSource<SurrealDbWsOkResponse>
{
	public string WsEngineId { get; }

	public SurrealWsTaskCompletionSource(string wsEngineId)
	{
		WsEngineId = wsEngineId;
	}
}
