using System.Globalization;
using System.Text;

namespace SurrealDb.Net.Internals.Extensions;

internal static class StringBuilderExtensions
{
    public static void AppendBytes(this StringBuilder builder, byte[] bytes)
    {
        builder.Append("'0x");

        for (int index = 0; index < bytes.Length; index++)
        {
            if (index > 31)
            {
                builder.Append("...");
                break;
            }

            builder.Append(bytes[index].ToString("X2", CultureInfo.InvariantCulture));
        }

        builder.Append('\'');
    }
}
