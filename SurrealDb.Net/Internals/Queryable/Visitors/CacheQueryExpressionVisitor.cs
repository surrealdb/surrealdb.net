using System.Linq.Expressions;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class CacheQueryExpressionVisitor : ExpressionVisitor
{
    private CachedQueryKey? _cachedQueryKey = null;

    public (Expression AfterCacheExpression, CachedQueryKey? CachedQueryKey) Bind(
        Expression expression
    )
    {
        return (Visit(expression), _cachedQueryKey);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(QueryableExtensions))
        {
            if (
                string.Equals(
                    node.Method.Name,
                    nameof(QueryableExtensions.Cached),
                    StringComparison.Ordinal
                )
            )
            {
                return BindCached(node);
            }
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindCached(MethodCallExpression node)
    {
        if (_cachedQueryKey.HasValue)
        {
            throw new InvalidOperationException("A query cannot be cached multiple times.");
        }

        string memberName = node.Arguments[1].ExtractConstant<string>()!;
        string sourceFilePath = node.Arguments[2].ExtractConstant<string>()!;
        int sourceLineNumber = node.Arguments[3].ExtractConstant<int>();

        _cachedQueryKey = new CachedQueryKey
        {
            MemberName = memberName,
            SourceFilePath = sourceFilePath,
            SourceLineNumber = sourceLineNumber,
        };

        return Visit(node.Arguments[0]);
    }
}
