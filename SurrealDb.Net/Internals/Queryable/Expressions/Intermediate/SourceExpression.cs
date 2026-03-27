using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal abstract class SourceExpression : IntermediateExpression
{
    public abstract SourceExpression MergeProjections(FieldsProjectionExpression fieldsProjection);
}

internal sealed class TableSourceExpression : SourceExpression
{
    public TableExpression Table { get; }

    public TableSourceExpression(TableExpression table)
    {
        Table = table;
    }

    public override SourceExpression MergeProjections(FieldsProjectionExpression fieldsProjection)
    {
        throw new NotSupportedException();
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Table);
        return this;
    }
}

internal sealed class SelectSourceExpression : SourceExpression
{
    public SelectExpression Select { get; }

    public SelectSourceExpression(SelectExpression select)
    {
        Select = select;
    }

    public override SourceExpression MergeProjections(FieldsProjectionExpression fieldsProjection)
    {
        return new SelectSourceExpression(
            Select.WithProjection(Select.Projection.ToFieldsProjection().Merge(fieldsProjection))
        );
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Select);
        return this;
    }
}
