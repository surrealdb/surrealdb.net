namespace SurrealDb.Net.LiveQuery.Tests.Abstract;

public abstract class BaseLiveQueryTests
{
    protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    protected Task WaitLiveQueryCreationAsync()
    {
        return Task.Delay(100);
    }

    protected Task WaitLiveQueryNotificationAsync()
    {
        return Task.Delay(100);
    }
}
