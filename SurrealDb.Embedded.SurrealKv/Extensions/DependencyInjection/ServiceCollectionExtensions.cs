using SurrealDb.Embedded.Internals;
using SurrealDb.Net.Internals;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register SurrealDB services for SurrealKV provider.
/// Registers <see cref="ISurrealDbKvEngine"/> as a factory instance (transient lifetime).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static SurrealDbBuilder AddSurrealKvProvider(this SurrealDbBuilder builder)
    {
        builder.Services.AddTransient<ISurrealDbKvEngine, SurrealDbEmbeddedEngine>();

        return builder;
    }
}
