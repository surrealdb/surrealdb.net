using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Expressions.Surreal;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class ToRecordIdExpressionVisitor : ExpressionVisitor
{
    private IReadOnlyDictionary<string, object?> _parameters = null!;

    public SurrealExpression Transform(
        SurrealExpression expression,
        IReadOnlyDictionary<string, object?> parameters
    )
    {
        _parameters = parameters;

        var currentExpression = (SurrealExpression)Visit(expression)!;

        while (true)
        {
            var revisitedExpression = (SurrealExpression)Visit(currentExpression)!;
            if (
                revisitedExpression == currentExpression
                || revisitedExpression is not SelectStatementExpression
            )
            {
                break;
            }

            currentExpression = revisitedExpression;
        }

        return currentExpression;
    }

    public override Expression? Visit(Expression? node)
    {
        return node switch
        {
            SelectStatementExpression selectStatement => VisitSelectStatement(selectStatement),
            SurrealExpression => node, // For all other SurrealExpression types, return as-is (no transformation needed)
            _ => base.Visit(node),
        };
    }

    private SelectStatementExpression VisitSelectStatement(
        SelectStatementExpression selectStatement
    )
    {
        // No WHERE condition, nothing to optimize
        if (selectStatement.Cond is null)
        {
            return selectStatement;
        }

        // Try to extract id == <recordId> from the WHERE condition
        var (recordIdParam, remainingCondition) = TryExtractRecordIdCondition(
            selectStatement.Cond.Value
        );

        if (recordIdParam is null)
        {
            return selectStatement;
        }

        if (
            _parameters.TryGetValue(recordIdParam, out var recordIdValue)
            && recordIdValue is RecordId recordId
        )
        {
            if (
                selectStatement.What.Values is [TableValueExpression tableValueExpression]
                && recordId.Table == tableValueExpression.Value
            ) // Record id selection must match the original table selection
            {
                return new SelectStatementExpression(
                    selectStatement.Fields,
                    WhatExpression.From(new RecordIdValueExpression(recordId)),
                    cond: remainingCondition is not null
                        ? new ConditionsExpression(remainingCondition)
                        : null,
                    selectStatement.Group,
                    selectStatement.Order,
                    selectStatement.Limit,
                    selectStatement.Start,
                    selectStatement.Only,
                    selectStatement.Explain
                );
            }

            if (
                selectStatement.What.Values is [RecordIdValueExpression recordIdValueExpression]
                && recordId == recordIdValueExpression.Value
            ) // Remove redundant record id selection
            {
                return new SelectStatementExpression(
                    selectStatement.Fields,
                    selectStatement.What,
                    cond: remainingCondition is not null
                        ? new ConditionsExpression(remainingCondition)
                        : null,
                    selectStatement.Group,
                    selectStatement.Order,
                    selectStatement.Limit,
                    selectStatement.Start,
                    selectStatement.Only,
                    selectStatement.Explain
                );
            }
        }

        return selectStatement;
    }

    /// <summary>
    /// Tries to extract an id == recordId condition from the given value expression.
    /// Only extracts if the condition is at the top level or connected via AND (not OR).
    /// Returns the parameter name for the record ID and the remaining condition (null if the id condition was the only one).
    /// Returns (null, null) if no id == recordId condition could be extracted.
    /// </summary>
    private (
        string? RecordIdParamName,
        ValueExpression? RemainingCondition
    ) TryExtractRecordIdCondition(ValueExpression condition)
    {
        // Direct match: condition is itself `id == <param>`
        if (IsIdEqualityCondition(condition, out var paramName))
        {
            return (paramName, null);
        }

        // AND tree: try to extract from an AND-only tree
        if (
            condition is BinaryValueExpression
            {
                Operator: SimpleOperator { Type: OperatorType.And }
            } binary
        )
        {
            return TryExtractFromAndTree(binary);
        }

        return (null, null);
    }

    private (string? RecordIdParamName, ValueExpression? RemainingCondition) TryExtractFromAndTree(
        BinaryValueExpression andExpression
    )
    {
        // Check left side
        if (IsIdEqualityCondition(andExpression.Left, out var leftParam))
        {
            return (leftParam, andExpression.Right);
        }

        // Check right side
        if (IsIdEqualityCondition(andExpression.Right, out var rightParam))
        {
            return (rightParam, andExpression.Left);
        }

        // Recurse into left AND sub-tree
        if (
            andExpression.Left is BinaryValueExpression
            {
                Operator: SimpleOperator { Type: OperatorType.And }
            } leftBinary
        )
        {
            var (param, remaining) = TryExtractFromAndTree(leftBinary);
            if (param is not null)
            {
                var newRemaining = remaining is null
                    ? andExpression.Right
                    : new BinaryValueExpression(
                        remaining,
                        new SimpleOperator(OperatorType.And),
                        andExpression.Right
                    );
                return (param, newRemaining);
            }
        }

        // Recurse into right AND sub-tree
        if (
            andExpression.Right is BinaryValueExpression
            {
                Operator: SimpleOperator { Type: OperatorType.And }
            } rightBinary
        )
        {
            var (param, remaining) = TryExtractFromAndTree(rightBinary);
            if (param is not null)
            {
                var newRemaining = remaining is null
                    ? andExpression.Left
                    : new BinaryValueExpression(
                        andExpression.Left,
                        new SimpleOperator(OperatorType.And),
                        remaining
                    );
                return (param, newRemaining);
            }
        }

        return (null, null);
    }

    private bool IsIdEqualityCondition(ValueExpression expression, out string? paramName)
    {
        paramName = null;

        if (expression is not BinaryValueExpression binary)
        {
            return false;
        }

        bool isEqualityOp = binary.Operator is SimpleOperator { Type: OperatorType.Exact };
        if (!isEqualityOp)
        {
            return false;
        }

        // Check left = id field, right = record ID param (or inverted)
        if (IsIdIdiom(binary.Left) && IsRecordIdParam(binary.Right, out paramName))
        {
            return true;
        }
        if (IsIdIdiom(binary.Right) && IsRecordIdParam(binary.Left, out paramName))
        {
            return true;
        }

        return false;
    }

    private static bool IsIdIdiom(ValueExpression expression)
    {
        return expression is IdiomValueExpression { Idiom.IsSingleFieldPart: true } idiom
            && idiom.Idiom.Parts[0] is FieldPartExpression { FieldName: "id" };
    }

    private bool IsRecordIdParam(ValueExpression expression, out string? paramName)
    {
        // Check if this parameter holds a RecordId value
        if (
            expression is ParameterValueExpression paramExpression
            && _parameters.TryGetValue(paramExpression.Name, out var value)
            && value is RecordId
        )
        {
            paramName = paramExpression.Name;
            return true;
        }

        paramName = null;
        return false;
    }
}
