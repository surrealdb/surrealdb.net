using System.Collections.Concurrent;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

internal sealed class Sessionizer : ISessionizer
{
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<Guid, ISessionInfo> _sessionInfos = new();
    private readonly Dictionary<Guid, Task> _pendingAttaches = new();
    private readonly AsyncLocal<Guid?> _attachingSessionId = new();

    public void Add(Guid sessionId, ISessionInfo sessionInfo)
    {
        _sessionInfos.TryAdd(sessionId, sessionInfo);
    }

    public Task EnsureAttachedAsync(Guid sessionId, Func<ISessionInfo, Task> attach)
    {
        // Requests sent by the attach itself (e.g. `use`, `signin`) must not wait on it
        if (_attachingSessionId.Value == sessionId)
        {
            return Task.CompletedTask;
        }

        ISessionInfo? sessionInfo;
        TaskCompletionSource<bool> tcs;

        // Claiming the session info and publishing the in-flight attach must be atomic,
        // otherwise a concurrent caller can see neither and skip the attach.
        lock (_gate)
        {
            if (_pendingAttaches.TryGetValue(sessionId, out var inFlight))
            {
                return inFlight;
            }

            if (!_sessionInfos.TryRemove(sessionId, out sessionInfo))
            {
                return Task.CompletedTask;
            }

            tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            _pendingAttaches[sessionId] = tcs.Task;
        }

        _ = RunAttachAsync(sessionId, sessionInfo, attach, tcs);
        return tcs.Task;
    }

    private async Task RunAttachAsync(
        Guid sessionId,
        ISessionInfo sessionInfo,
        Func<ISessionInfo, Task> attach,
        TaskCompletionSource<bool> tcs
    )
    {
        _attachingSessionId.Value = sessionId;

        try
        {
            await attach(sessionInfo).ConfigureAwait(false);
            RemovePendingAttach(sessionId, tcs.Task);
            tcs.TrySetResult(true);
        }
        catch (Exception ex)
        {
            RemovePendingAttach(sessionId, tcs.Task);
            tcs.TrySetException(ex);
        }
    }

    private void RemovePendingAttach(Guid sessionId, Task task)
    {
        lock (_gate)
        {
            if (_pendingAttaches.TryGetValue(sessionId, out var current) && current == task)
            {
                _pendingAttaches.Remove(sessionId);
            }
        }
    }

    public void TryRemove(Guid sessionId)
    {
        _sessionInfos.TryRemove(sessionId, out _);
        lock (_gate)
        {
            _pendingAttaches.Remove(sessionId);
        }
    }
}
