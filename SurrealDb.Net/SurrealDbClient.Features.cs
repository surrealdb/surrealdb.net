using SurrealDb.Net.Extensions;

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient
{
    public async Task<bool> SupportsSession(CancellationToken cancellationToken = default)
    {
        var version = await Engine.Version(cancellationToken).ConfigureAwait(false);
        return version.ToSemver().Major >= 3;
    }

    public async Task<bool> SupportsTransactions(CancellationToken cancellationToken = default)
    {
        bool isWebsocket = Uri.Scheme == "ws" || Uri.Scheme == "wss";
        if (!isWebsocket)
        {
            return false;
        }

        var version = await Engine.Version(cancellationToken).ConfigureAwait(false);
        return version.ToSemver().Major >= 3;
    }
}
