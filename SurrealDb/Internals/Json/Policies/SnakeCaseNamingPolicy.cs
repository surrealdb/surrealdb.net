using SurrealDb.Internals.Extensions;
using System.Text.Json;

namespace SurrealDb.Internals.Json.Policies;

internal class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

    public override string ConvertName(string name)
    {
        return name.ToSnakeCase();
    }
}
