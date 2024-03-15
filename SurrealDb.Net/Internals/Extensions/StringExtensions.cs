using System.Text.Json;

namespace SurrealDb.Net.Internals.Extensions;

internal static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(str);
    }

    public static string ToCamelCase(this string str)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(str);
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
