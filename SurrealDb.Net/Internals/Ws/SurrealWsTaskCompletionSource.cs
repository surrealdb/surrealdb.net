namespace SurrealDb.Net.Internals.Ws;

internal class SurrealWsTaskCompletionSource : TaskCompletionSource<SurrealDbWsOkResponse>
{
    public SurrealDbWsRequestPriority Priority { get; }

    public SurrealWsTaskCompletionSource(SurrealDbWsRequestPriority priority)
    {
        Priority = priority;
    }
}
