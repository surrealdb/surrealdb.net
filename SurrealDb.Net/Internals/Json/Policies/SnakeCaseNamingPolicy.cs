#if !NET8_0_OR_GREATER
using SurrealDb.Net.Internals.Extensions;
using System.Text.Json;

namespace SurrealDb.Net.Internals.Json.Policies;

internal class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

    public override string ConvertName(string name)
    {
        return name.ToSnakeCase();
    }
}
#endif
