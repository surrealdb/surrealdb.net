namespace SurrealDb.Net.Internals.Extensions;

internal static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(
                str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())
            )
            .ToLower();
    }

    public static bool IsValidVariableName(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return false;
        }

        foreach (char c in str)
        {
            if (c == ' ')
            {
                return false;
            }
        }

        return true;
    }
}
