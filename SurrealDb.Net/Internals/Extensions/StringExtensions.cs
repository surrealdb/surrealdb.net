namespace SurrealDb.Net.Internals.Extensions;

internal static class StringExtensions
{
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
