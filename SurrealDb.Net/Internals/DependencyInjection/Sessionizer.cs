using System.Collections.Concurrent;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

internal sealed class Sessionizer : ISessionizer
{
    private readonly ConcurrentDictionary<Guid, ISessionInfo> _sessionInfos = new();
    private readonly ConcurrentDictionary<Guid, Task> _pendingAttaches = new();
    private readonly AsyncLocal<Guid?> _attachingSessionId = new();

    public void Add(Guid sessionId, ISessionInfo sessionInfo)
    {
        _sessionInfos.TryAdd(sessionId, sessionInfo);
    }

    public Task EnsureAttachedAsync(Guid sessionId, Func<ISessionInfo, Task> attach)
    {
        if (_attachingSessionId.Value == sessionId)
        {
            return Task.CompletedTask;
        }

        // At most one caller ever gets the sessionInfo for a given sessionId
        if (_sessionInfos.TryRemove(sessionId, out var sessionInfo))
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingAttaches[sessionId] = tcs.Task;
            _ = RunAttachAsync(sessionId, sessionInfo, attach, tcs);
            return tcs.Task;
        }

        // Someone else already claimed it: wait for their in-flight attach, if any
        return _pendingAttaches.TryGetValue(sessionId, out var inFlight)
            ? inFlight
            : Task.CompletedTask;
    }

    private async Task RunAttachAsync(
        Guid sessionId,
        ISessionInfo sessionInfo,
        Func<ISessionInfo, Task> attach,
        TaskCompletionSource tcs
    )
    {
        _attachingSessionId.Value = sessionId;

        try
        {
            await attach(sessionInfo).ConfigureAwait(false);
            tcs.TrySetResult();
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        finally
        {
            _pendingAttaches.TryRemove(sessionId, out _);
        }
    }

    public void TryRemove(Guid sessionId)
    {
        _sessionInfos.Remove(sessionId, out _);
        _pendingAttaches.TryRemove(sessionId, out _);
    }
}
