using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class ExtractParametersExpressionVisitor : ExpressionVisitor
{
    private HashSet<string> _cachedQueryParameters = null!;
    private readonly Dictionary<string, object?> _extractedParameters = [];

    public IReadOnlyDictionary<string, object?> Bind(
        Expression expression,
        HashSet<string> cachedQueryParameters
    )
    {
        _cachedQueryParameters = cachedQueryParameters;
        Visit(expression);
        return _extractedParameters;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (
            node
                is {
                    Expression: ConstantExpression constantExpression,
                    Member.DeclaringType: not null
                }
            && IsExternalParameter(node.Member.DeclaringType)
        )
        {
            string paramName = node.Member.Name;

            if (_cachedQueryParameters.Contains(paramName))
            {
                object? value = constantExpression
                    .Value?.GetType()
                    .GetField(node.Member.Name)
                    ?.GetValue(constantExpression.Value);

                _extractedParameters[paramName] = value;
            }

            return node;
        }

        return base.VisitMember(node);
    }

    private static bool IsExternalParameter(Type type)
    {
        return type.IsSealed
            && type.BaseType == typeof(object)
            && !type.IsPublic
            && type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }
}
