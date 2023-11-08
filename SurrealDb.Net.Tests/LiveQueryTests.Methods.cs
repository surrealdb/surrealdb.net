namespace SurrealDb.Net.Tests;

public partial class LiveQueryTests
{
    private Task WaitLiveQueryNotificationAsync()
    {
        return Task.Delay(100);
    }
}
