using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals;

internal static class SurrealDbEngineHelpers
{
    public static bool ShouldUseCbor(SurrealDbClientParams parameters)
    {
        return parameters.Serialization?.ToLowerInvariant() != SerializationConstants.JSON;
    }
}
