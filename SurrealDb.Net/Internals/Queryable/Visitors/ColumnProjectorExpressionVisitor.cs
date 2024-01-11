using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal class ColumnProjectorExpressionVisitor : SurrealExpressionVisitor
{
    private readonly NominatorExpressionVisitor _nominator;
    private Dictionary<ColumnExpression, ColumnExpression>? _map;
    private List<ColumnDeclaration>? _columns;
    private HashSet<string>? _columnNames;
    private HashSet<Expression>? _candidates;
    private string? _existingAlias;
    private string? _newAlias;

    internal ColumnProjectorExpressionVisitor(Func<Expression, bool> fnCanBeColumn)
    {
        _nominator = new NominatorExpressionVisitor(fnCanBeColumn);
    }

    internal ProjectedColumns ProjectColumns(
        Expression expression,
        string newAlias,
        string existingAlias
    )
    {
        _map = [];
        _columns = [];
        _columnNames = [];
        _newAlias = newAlias;
        _existingAlias = existingAlias;
        _candidates = _nominator.Nominate(expression);

        return new ProjectedColumns(Visit(expression)!, _columns.AsReadOnly());
    }
}
