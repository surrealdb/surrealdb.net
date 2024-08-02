using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals;

internal static class SurrealDbEngineHelpers
{
    public static bool ShouldUseCbor(SurrealDbOptions configuration)
    {
        return configuration.Serialization?.ToLowerInvariant() != SerializationConstants.JSON;
    }
}
