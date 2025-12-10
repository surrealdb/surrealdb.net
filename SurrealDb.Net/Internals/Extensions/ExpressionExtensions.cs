using System.Linq.Expressions;
using System.Text.RegularExpressions;
using SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

namespace SurrealDb.Net.Internals.Extensions;

internal static class ExpressionExtensions
{
    public static ValueExpression ToValue(this Expression expression)
    {
        return expression switch
        {
            ValueExpression valueExpression => valueExpression,
            IdiomExpression idiomExpression => new IdiomValueExpression(idiomExpression),
            SelectStatementExpression selectStatementExpression => new SubqueryValueExpression(
                selectStatementExpression
            ),
            _ => throw new NotSupportedException(
                $"Cannot convert {expression.Type.Name} to {nameof(ValueExpression)}."
            ),
        };
    }

    public static IdiomExpression ToIdiom(this Expression expression)
    {
        return expression switch
        {
            IdiomExpression idiomExpression => idiomExpression,
            IdiomValueExpression idiomValueExpression => idiomValueExpression.Idiom,
            ValueExpression valueExpression => new IdiomExpression(
                [new StartPartExpression(valueExpression)]
            ),
            _ => throw new NotSupportedException(
                $"Cannot convert {expression.Type.Name} to {nameof(IdiomExpression)}."
            ),
        };
    }

    public static ValueExpression ToRegex(this ValueExpression expression)
    {
        return expression switch
        {
            RegexValueExpression regexValueExpression => regexValueExpression,
#pragma warning disable MA0009
            StringValueExpression stringValueExpression => new RegexValueExpression(
                new Regex(stringValueExpression.Value)
            ),
#pragma warning restore MA0009
            _ => throw new NotSupportedException(
                $"Cannot convert {expression.Type.Name} to {nameof(RegexValueExpression)}."
            ),
        };
    }

    public static IdiomExpression ToFieldIdiom(this string alias)
    {
        return new IdiomExpression([new FieldPartExpression(alias)]);
    }

    public static T? ExtractConstant<T>(this Expression expression)
    {
        if (expression.Type != typeof(T))
        {
            throw new InvalidCastException(
                $"The expression cannot be converted into a {typeof(T).Name}."
            );
        }
        if (expression is not ConstantExpression constantExpression)
        {
            throw new InvalidCastException("The expression is not a constant expression.");
        }

        return (T?)constantExpression.Value;
    }
}
