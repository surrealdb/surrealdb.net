#if NET6_0_OR_GREATER
using SurrealDb.Net.Handlers;
#endif

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
    /// Logger category for method execution, excluding <see cref="ISurrealDbSharedMethods.Connect(CancellationToken)"/>.
    /// </summary>
    public sealed class Method : LoggerCategory<Method>;

#if NET6_0_OR_GREATER
    /// <summary>
    /// Logger category for messages related to written or generated queries, that can be executed within
    /// <see cref="ISurrealDbSharedMethods.Query(QueryInterpolatedStringHandler, CancellationToken)" />
    /// or <see cref="ISurrealDbSharedMethods.RawQuery(string, IReadOnlyDictionary{string, object?}?, CancellationToken)"/>.
    /// </summary>
#else
    /// <summary>
    /// Logger category for messages related to written or generated queries, that can be executed within
    /// <see cref="ISurrealDbSharedMethods.Query(FormattableString, CancellationToken)" />
    /// or <see cref="ISurrealDbSharedMethods.RawQuery(string, IReadOnlyDictionary{string, object?}?, CancellationToken)"/>.
    /// </summary>
#endif
    public sealed class Query : LoggerCategory<Query>;

    /// <summary>
    /// Logger category for data serialization and deserialization,
    /// e.g. hexa CBOR format exchanged between the client and a SurrealDB instance.
    /// </summary>
    public sealed class Serialization : LoggerCategory<Serialization>;
}
