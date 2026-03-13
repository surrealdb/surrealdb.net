using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Embedded.Internals;

internal sealed class EmbeddedSessionInfo : SessionInfo
{
    public EmbeddedSessionInfo() { }

    public EmbeddedSessionInfo(EmbeddedSessionInfo from)
        : base(from) { }

    public EmbeddedSessionInfo(SurrealDbOptions options)
        : base(options) { }
}
