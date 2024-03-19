namespace SurrealDb.Net.Internals.Constants;

internal static class NamingPolicyConstants
{
    public const string LOWER = "lower";
    public const string UPPER = "upper";

    public const string CAMEL_CASE = "camelcase";
    public const string SNAKE_CASE = "snakecase";
    public const string SNAKE_CASE_LOWER = $"{SNAKE_CASE}{LOWER}";
    public const string SNAKE_CASE_UPPER = $"{SNAKE_CASE}{UPPER}";
    public const string KEBAB_CASE = "kebabcase";
    public const string KEBAB_CASE_LOWER = $"{KEBAB_CASE}{LOWER}";
    public const string KEBAB_CASE_UPPER = $"{KEBAB_CASE}{UPPER}";
}
