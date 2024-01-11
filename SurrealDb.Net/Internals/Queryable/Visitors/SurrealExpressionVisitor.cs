using System.Collections.ObjectModel;
using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal abstract class SurrealExpressionVisitor : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is SurrealExpression surrealExpression)
        {
            return surrealExpression.SurrealNodeType switch
            {
                SurrealExpressionType.Table => VisitTable((TableExpression)node),
                SurrealExpressionType.Column => VisitColumn((ColumnExpression)node),
                SurrealExpressionType.Select => VisitSelect((SelectExpression)node),
                SurrealExpressionType.Projection => VisitProjection((ProjectionExpression)node),
                _ => base.Visit(node),
            };
        }

        return base.Visit(node);
    }

    protected virtual Expression VisitTable(TableExpression table)
    {
        return table;
    }

    protected virtual Expression VisitColumn(ColumnExpression column)
    {
        return column;
    }

    protected virtual Expression VisitSelect(SelectExpression node)
    {
        var columns = VisitColumnDeclarations(node.Columns);
        var from = VisitSource(node.From);
        var where = Visit(node.Where);
        var groupBy = Visit(node.GroupBy); // TODO
        var orderBy = VisitOrderBy(node.OrderBy);
        var start = Visit(node.Start);
        var limit = Visit(node.Limit);

        if (
            columns != node.Columns
            || from != node.From
            || where != node.Where
            || start != node.Start
            || limit != node.Limit
        )
        {
            return new SelectExpression(
                node.Type,
                node.Alias,
                columns,
                from!,
                where,
                groupBy,
                orderBy,
                start,
                limit
            );
        }

        return node;
    }

    protected virtual Expression? VisitSource(Expression node)
    {
        return Visit(node);
    }

    protected virtual Expression VisitProjection(ProjectionExpression node)
    {
        var source = Visit(node.Source) as SelectExpression;
        var projector = Visit(node.Projector);

        if (source != node.Source || projector != node.Projector)
        {
            return new ProjectionExpression(source!, projector!);
        }

        return node;
    }

    protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(
        ReadOnlyCollection<ColumnDeclaration> expressions
    )
    {
        List<ColumnDeclaration>? alternate = null;

        for (int i = 0; i < expressions.Count; i++)
        {
            var columnDeclaration = expressions[i];
            var expression = Visit(columnDeclaration.Expression)!;

            if (alternate is null && expression != columnDeclaration.Expression)
            {
                alternate = new List<ColumnDeclaration>(expressions.Count);
                alternate.AddRange(expressions.Take(i));
            }

            alternate?.Add(new(columnDeclaration.Name, expression));
        }

        return alternate is not null ? alternate.AsReadOnly() : expressions;
    }

    protected ReadOnlyCollection<OrderExpression> VisitOrderBy(
        ReadOnlyCollection<OrderExpression> expressions
    )
    {
        List<OrderExpression>? alternate = null;

        for (int i = 0; i < expressions.Count; i++)
        {
            var orderExpression = expressions[i];
            var expression = Visit(orderExpression.Expression)!;

            if (alternate is null && expression != orderExpression.Expression)
            {
                alternate = new List<OrderExpression>(expressions.Count);
                alternate.AddRange(expressions.Take(i));
            }

            alternate?.Add(new OrderExpression(orderExpression.OrderType, expression));
        }

        return alternate is not null ? alternate.AsReadOnly() : expressions;
    }
}
