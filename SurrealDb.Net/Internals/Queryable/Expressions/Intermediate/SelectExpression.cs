using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class SelectExpression : IntermediateExpression
{
    public SourceExpression Source { get; private init; }
    public ProjectionExpression Projection { get; private init; }
    public WhereExpression? Where { get; private init; }
    public GroupsExpression? Groups { get; private init; }
    public OrdersExpression? Orders { get; private init; }
    public TakeExpression? Take { get; private init; }
    public SkipExpression? Skip { get; private init; }
    public bool SingleValue { get; private init; }

    private SelectExpression(SelectExpression from)
        : this(from, from.Type) { }

    private SelectExpression(SelectExpression from, Type outputType)
        : base(outputType)
    {
        Source = from.Source;
        Projection = from.Projection;
        Where = from.Where;
        Groups = from.Groups;
        Orders = from.Orders;
        Take = from.Take;
        Skip = from.Skip;
        SingleValue = from.SingleValue;
    }

    public SelectExpression(Type resultType, TableExpression table, ProjectionExpression projection)
        : base(resultType)
    {
        Source = new TableSourceExpression(table);
        Projection = projection;
    }

    public SelectExpression(
        Type resultType,
        SelectExpression select,
        ProjectionExpression? projection = null
    )
        : base(resultType)
    {
        Source = new SelectSourceExpression(select);
        Projection = projection ?? select.Projection;
    }

    public SelectExpression WithSource(SourceExpression source)
    {
        return new SelectExpression(this) { Source = source };
    }

    public SelectExpression WithProjection(ProjectionExpression projection)
    {
        return new SelectExpression(this, projection.Type) { Projection = projection };
    }

    public SelectExpression AppendWhere(Expression predicate)
    {
        var innerWhereExpression = Where is not null
            ? AndAlso(Where.Expression, predicate)
            : predicate;
        return new SelectExpression(this) { Where = new(innerWhereExpression) };
    }

    public SelectExpression WithGroup(Expression expression)
    {
        return new SelectExpression(this) { Groups = new GroupsExpression(expression) };
    }

    public SelectExpression WithGroupAll()
    {
        return new SelectExpression(this) { Groups = new GroupsExpression(null) };
    }

    public SelectExpression WithOrder(OrderByInfo value)
    {
        return new SelectExpression(this) { Orders = new OrdersExpression([value]) };
    }

    public SelectExpression AppendOrder(OrderByInfo value)
    {
        return Orders is null
            ? WithOrder(value)
            : new SelectExpression(this)
            {
                Orders = new OrdersExpression([.. Orders.Infos, value]),
            };
    }

    public SelectExpression WithTake(Expression value)
    {
        return new SelectExpression(this) { Take = new(value) };
    }

    public SelectExpression WithSkip(Expression value)
    {
        return new SelectExpression(this) { Skip = new(value) };
    }

    public SelectExpression WithSingleValue()
    {
        return new SelectExpression(this) { SingleValue = true };
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Projection);
        visitor.Visit(Where);
        visitor.Visit(Orders);
        visitor.Visit(Take);
        visitor.Visit(Skip);

        return this;
    }
}
