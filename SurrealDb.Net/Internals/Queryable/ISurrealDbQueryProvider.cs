using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable;

internal interface ISurrealDbQueryProvider : IQueryProvider
{
    (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression
    );
}
