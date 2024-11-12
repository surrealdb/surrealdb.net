using SurrealDb.Embedded.Internals;
using SurrealDb.Net.Internals;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register SurrealDB services for in-memory provider.
/// Registers <see cref="ISurrealDbInMemoryEngine"/> as a factory instance (transient lifetime).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static SurrealDbBuilder AddInMemoryProvider(this SurrealDbBuilder builder)
    {
        builder.Services.AddTransient<ISurrealDbInMemoryEngine, SurrealDbEmbeddedEngine>();

        return builder;
    }
}
