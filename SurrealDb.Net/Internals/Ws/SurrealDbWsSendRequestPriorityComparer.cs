namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsSendRequestPriorityComparer : IComparer<SurrealDbWsSendRequest>
{
    public int Compare(SurrealDbWsSendRequest? x, SurrealDbWsSendRequest? y)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return y.Priority.CompareTo(x.Priority);
    }
}
