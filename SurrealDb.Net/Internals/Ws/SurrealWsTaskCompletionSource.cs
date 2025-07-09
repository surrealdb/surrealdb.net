namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealWsTaskCompletionSource : TaskCompletionSource<SurrealDbWsOkResponse>
{
#if NET9_0_OR_GREATER
    public SurrealWsTaskCompletionSource(TaskCreationOptions options)
        : base(options) { }
#else
    public SurrealDbWsRequestPriority Priority { get; }

    public SurrealWsTaskCompletionSource(
        TaskCreationOptions options,
        SurrealDbWsRequestPriority priority
    )
        : base(options)
    {
        Priority = priority;
    }
#endif
}
