using Microsoft.Extensions.ObjectPool;

namespace SurrealDb.Net.Internals.ObjectPool;

internal class SurrealDbClientPoolContainer : IResettable
{
    public ISurrealDbEngine? ClientEngine { get; set; }

    public bool TryReset()
    {
        if (ClientEngine is null)
        {
            return false;
        }

        bool isReset = ClientEngine.TryReset();
        if (!isReset)
        {
            ClientEngine.Dispose();
        }

        return isReset;
    }
}
