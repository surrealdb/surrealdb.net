using Microsoft.IO;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsSendRequest
{
    public SurrealDbWsRequest Content { get; }
    public SurrealDbWsRequestPriority Priority { get; }
    public SurrealWsTaskCompletionSource CompletionSource { get; }
    public RecyclableMemoryStream Stream { get; }
    public WeakReference<SurrealDbWsEngine> WsEngine { get; }
    public CancellationToken CancellationToken { get; }

    public SurrealDbWsSendRequest(
        SurrealDbWsRequest content,
        SurrealDbWsRequestPriority priority,
        SurrealWsTaskCompletionSource completionSource,
        RecyclableMemoryStream stream,
        WeakReference<SurrealDbWsEngine> wsEngine,
        CancellationToken cancellationToken
    )
    {
        Content = content;
        Priority = priority;
        CompletionSource = completionSource;
        Stream = stream;
        WsEngine = wsEngine;
        CancellationToken = cancellationToken;
    }
}
