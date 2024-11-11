using SurrealDb.Embedded.Internals;
using SurrealDb.Net.Internals;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register SurrealDB services for RocksDb provider.
/// Registers <see cref="ISurrealDbRocksDbEngine"/> as a factory instance (transient lifetime).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static SurrealDbBuilder AddRocksDbProvider(this SurrealDbBuilder builder)
    {
        builder.Services.AddTransient<ISurrealDbRocksDbEngine, SurrealDbEmbeddedEngine>();

        return builder;
    }
}
