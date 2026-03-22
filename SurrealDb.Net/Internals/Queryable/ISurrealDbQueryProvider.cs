using System.Linq.Expressions;
using Semver;

namespace SurrealDb.Net.Internals.Queryable;

internal interface ISurrealDbQueryProvider : IQueryProvider
{
    (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression,
        SemVersion version
    );
}
