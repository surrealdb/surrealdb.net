namespace SurrealDb.Net.Internals.Logging;

internal static class DbLoggerCategory
{
    /// <summary>
    /// The prefix for all logger categories.
    /// </summary>
    public const string Name = "SurrealDB";

    /// <summary>
    /// Logger category for messages related to connection operations.
    /// </summary>
    public sealed class Connection : LoggerCategory<Connection>;

    /// <summary>
    /// Logger category for method execution, excluding <see cref="SurrealDbClient.Connect(CancellationToken)"/>.
    /// </summary>
    public sealed class Method : LoggerCategory<Method>;

    /// <summary>
    /// Logger category for messages related to written or generated queries, that can be executed within
    /// <see cref="SurrealDbClient.Query(FormattableString, CancellationToken)" />
    /// or <see cref="SurrealDbClient.RawQuery(string, IReadOnlyDictionary{string, object?}?, CancellationToken)"/>.
    /// </summary>
    public sealed class Query : LoggerCategory<Query>;
}
