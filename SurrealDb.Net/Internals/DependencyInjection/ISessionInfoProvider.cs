using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

public interface ISessionInfoProvider
{
    ISessionInfo Get(SurrealDbOptions options);
}
