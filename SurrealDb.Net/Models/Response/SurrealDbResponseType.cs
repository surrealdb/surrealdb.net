namespace SurrealDb.Net.Models.Response;

public enum SurrealDbResponseType
{
    Other,
    Live,
    Kill,
}

internal static class SurrealDbResponseTypeExtensions
{
    public static SurrealDbResponseType? From(string? value)
    {
        if (
            string.Equals(
                value,
                nameof(SurrealDbResponseType.Live),
                StringComparison.OrdinalIgnoreCase
            )
        )
            return SurrealDbResponseType.Live;
        if (
            string.Equals(
                value,
                nameof(SurrealDbResponseType.Kill),
                StringComparison.OrdinalIgnoreCase
            )
        )
            return SurrealDbResponseType.Kill;
        return null;
    }
}
