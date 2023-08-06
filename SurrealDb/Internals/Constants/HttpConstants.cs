namespace SurrealDb.Internals.Constants;

internal static class HttpConstants
{
	public const string ACCEPT_HEADER_NAME = "Accept";
	public const string NS_HEADER_NAME = "NS";
	public const string DB_HEADER_NAME = "DB";

	public static readonly string[] ACCEPT_HEADER_VALUES = new[] { "application/json" };
}
