namespace SurrealDb.Net.Internals.Helpers;

internal class HttpClientHelper
{
    public static string GetHttpClientName(Uri uri) => $"[SurrealDB] {uri.AbsoluteUri}";
}
