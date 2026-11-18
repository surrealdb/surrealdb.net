using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

public interface ISessionizer
{
    void Add(Guid sessionId, ISessionInfo sessionInfo);

    /// <summary>
    /// Runs <paramref name="attach"/> exactly once for a not-yet-attached session, no matter how many callers race to be the first to use it.
    /// Concurrent callers await the same in-flight attach instead of each triggering their own.
    /// </summary>
    Task EnsureAttachedAsync(Guid sessionId, Func<ISessionInfo, Task> attach);

    void TryRemove(Guid sessionId);
}
