namespace SurrealDb.Net.Internals.Helpers;

internal static class HttpClientHelper
{
    public static string GetHttpClientName(Uri uri) => $"[SurrealDB] {uri.AbsoluteUri}";
}
