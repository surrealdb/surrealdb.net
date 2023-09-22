namespace SurrealDb.Models.Response;

/// <summary>
/// A SurrealDB unknown result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbUnknownResult : ISurrealDbErrorResult
{
	public bool IsOk => false;
}
