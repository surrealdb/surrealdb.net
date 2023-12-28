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

    public static bool IsAlphanumeric(this string str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
