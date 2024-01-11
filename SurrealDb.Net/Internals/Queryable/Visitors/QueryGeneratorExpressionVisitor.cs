using System.Linq.Expressions;
using System.Text;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Internals.Queryable.Expressions;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal class QueryGeneratorExpressionVisitor : SurrealExpressionVisitor
{
    private StringBuilder _surqlQueryBuilder = null!;

    public string Translate(Expression expression)
    {
        _surqlQueryBuilder = new StringBuilder();

        //_surqlQueryBuilder.Append($"SELECT * FROM {fromTable}");

        Visit(expression);

        return _surqlQueryBuilder.ToString();
    }

    private static Expression StripQuotes(Expression node)
    {
        while (node.NodeType == ExpressionType.Quote)
        {
            node = ((UnaryExpression)node).Operand;
        }

        return node;
    }

    protected override Expression VisitSelect(SelectExpression node)
    {
        _surqlQueryBuilder.Append("SELECT ");
        _surqlQueryBuilder.Append('*');

        //foreach (var column in node.Columns)
        //{
        //    Visit(column.Expression);
        //    _surqlQueryBuilder.Append(" AS ");
        //    _surqlQueryBuilder.Append(column.Name);
        //    _surqlQueryBuilder.Append(", ");
        //}

        _surqlQueryBuilder.Append(" FROM ");
        Visit(node.From);
        //_surqlQueryBuilder.Append(_fromTable);

        if (node.Where is not null)
        {
            _surqlQueryBuilder.Append(" WHERE ");
            Visit(node.Where);
        }

        if (node.GroupBy.Count > 0)
        {
            _surqlQueryBuilder.Append(" GROUP BY ");
            for (int index = 0; index < node.GroupBy.Count; index++)
            {
                var groupNode = node.GroupBy[index];
                Visit(groupNode);

                if (index < node.GroupBy.Count - 1)
                {
                    _surqlQueryBuilder.Append(", ");
                }
            }
        }
        //if (node.GroupBy is not null)
        //{
        //    _surqlQueryBuilder.Append(" GROUP BY ");
        //    Visit(node.GroupBy);
        //}

        if (node.OrderBy.Count > 0)
        {
            _surqlQueryBuilder.Append(" ORDER BY ");
            for (int index = 0; index < node.OrderBy.Count; index++)
            {
                var orderNode = node.OrderBy[index];
                Visit(orderNode.Expression);

                if (orderNode.OrderType == OrderType.Descending)
                {
                    _surqlQueryBuilder.Append(" DESC");
                }

                if (index < node.OrderBy.Count - 1)
                {
                    _surqlQueryBuilder.Append(", ");
                }
            }
        }

        if (node.Limit is not null)
        {
            _surqlQueryBuilder.Append(" LIMIT ");
            Visit(node.Limit);
        }

        if (node.Start is not null)
        {
            _surqlQueryBuilder.Append(" START ");
            Visit(node.Start);
        }

        return node;
    }

    protected override Expression VisitTable(TableExpression node)
    {
        _surqlQueryBuilder.Append(node.Name);
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _surqlQueryBuilder.Append('(');
        Visit(node.Left);

        switch (node.NodeType)
        {
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                _surqlQueryBuilder.Append(" && ");
                break;
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                _surqlQueryBuilder.Append(" || ");
                break;
            case ExpressionType.GreaterThan:
                _surqlQueryBuilder.Append(" > ");
                break;
            case ExpressionType.GreaterThanOrEqual:
                _surqlQueryBuilder.Append(" >= ");
                break;
            case ExpressionType.LessThan:
                _surqlQueryBuilder.Append(" < ");
                break;
            case ExpressionType.LessThanOrEqual:
                _surqlQueryBuilder.Append(" <= ");
                break;
            case ExpressionType.Equal:
                _surqlQueryBuilder.Append(" == ");
                break;
            case ExpressionType.NotEqual:
                _surqlQueryBuilder.Append(" != ");
                break;
            case ExpressionType.Coalesce:
                _surqlQueryBuilder.Append(" ?? ");
                break;
            case ExpressionType.Add:
                _surqlQueryBuilder.Append(" + ");
                break;
            case ExpressionType.Subtract:
                _surqlQueryBuilder.Append(" - ");
                break;
            case ExpressionType.Multiply:
                _surqlQueryBuilder.Append(" * ");
                break;
            case ExpressionType.Divide:
                _surqlQueryBuilder.Append(" / ");
                break;
            case ExpressionType.Modulo:
                _surqlQueryBuilder.Append(" % ");
                break;
            default:
                throw new NotSupportedException(
                    string.Format("The binary operator '{0}' is not supported", node.NodeType)
                );
        }

        Visit(node.Right);
        _surqlQueryBuilder.Append(')');

        return node;
        //return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        return base.VisitUnary(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var output = node.Value switch
        {
            null => "null",
            bool value => value ? "true" : "false",
            int value => value.ToString(),
            string value => $"\"{value}\"",
            _ => throw new NotSupportedException(
                string.Format("The constant '{0}' is not supported", node.Value)
            ),
        };

        _surqlQueryBuilder.Append(output);

        return node;
        //return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.MemberAccess:
                _surqlQueryBuilder.Append(node.Member.Name);
                break;
            default:
                throw new NotSupportedException(
                    string.Format(
                        "The member operation '{0}' on '{1}' is not supported",
                        node.NodeType,
                        node.Member.Name
                    )
                );
        }

        return node;
        //return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method)
        {
            //case var method when method.Name == "Contains":
            //    _surqlQueryBuilder.Append(" CONTAINS ");
            //    Visit(node.Object);
            //    _surqlQueryBuilder.Append(", ");
            //    Visit(node.Arguments[0]);
            //    break;
            //case var method
            //    when method.DeclaringType == typeof(System.Linq.Queryable)
            //        && method.Name == "Select":
            //    _surqlQueryBuilder.Append("SELECT ");
            //    Visit(node.Arguments[1]);
            //    break;
            //case var method
            //    when method.DeclaringType == typeof(System.Linq.Queryable)
            //        && method.Name == "Where":
            //    _surqlQueryBuilder.Append(" WHERE ");
            //    LambdaExpression lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
            //    Visit(lambda.Body);
            //    //Visit(node.Arguments[1]);
            //    break;
            case var method
                when string.Equals(method.Name, "FromDays", StringComparison.Ordinal)
                    && node.Type == typeof(TimeSpan):
                var timeSpan = TimeSpan.FromDays(
                    (double)((ConstantExpression)node.Arguments[0]).Value!
                );
                var (seconds, nanos) = TimeSpanFormatter.Convert(timeSpan);
                var duration = new Duration(seconds, nanos);
                _surqlQueryBuilder.Append(duration.ToString());
                //_surqlQueryBuilder.Append(" FROMDAYS ");
                //Visit(node.Arguments[0]);
                break;
            default:
                throw new NotSupportedException(
                    string.Format("The method call '{0}' is not supported", node.Method.Name)
                );
        }

        return node;
        //return base.VisitMethodCall(node);
    }

    //private void ExpectSelectStatement()
    //{
    //    if (_surqlQueryBuilder.Length <= 0)
    //    {
    //        _surqlQueryBuilder.Append($"SELECT * FROM {_fromTable}");
    //    }
    //}
}
