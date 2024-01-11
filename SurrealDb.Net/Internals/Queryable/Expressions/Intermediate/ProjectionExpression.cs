using System.Collections.Immutable;
using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal abstract class ProjectionExpression : IntermediateExpression
{
    public bool CanBeSingle { get; }
    public abstract Expression? InnerExpression { get; }

    protected ProjectionExpression(Type resultType, bool canBeSingle = true)
        : base(resultType)
    {
        CanBeSingle = canBeSingle;
    }

    /// <summary>
    /// Tries to set <see cref="ProjectionExpression.CanBeSingle"/> to <see langword="true"/>.
    /// </summary>
    public abstract ProjectionExpression TrySingle();

    /// <summary>
    /// Sets <see cref="ProjectionExpression.CanBeSingle"/> to <see langword="false"/>.
    /// </summary>
    public abstract ProjectionExpression Unsingle();

    public abstract FieldsProjectionExpression ToFieldsProjection();
}

internal sealed class ExpressionProjectionExpression : ProjectionExpression
{
    public Expression? Expression { get; }
    public override Expression? InnerExpression => Expression;
    public bool IsSelfProjection => Expression is null;

    private ExpressionProjectionExpression(
        Type resultType,
        Expression? expression = null,
        bool canBeSingle = true
    )
        : base(resultType, canBeSingle)
    {
        Expression = expression;
    }

    public static ExpressionProjectionExpression All(Type resultType)
    {
        return new(resultType, canBeSingle: false);
    }

    public static ExpressionProjectionExpression From(
        Type resultType,
        Expression expression,
        bool canBeSingle = true
    )
    {
        return new(resultType, expression, canBeSingle);
    }

    public override ProjectionExpression TrySingle()
    {
        return CanBeSingle
            ? this
            : new ExpressionProjectionExpression(Type, Expression, canBeSingle: true);
    }

    public override ProjectionExpression Unsingle()
    {
        return CanBeSingle
            ? new ExpressionProjectionExpression(Type, Expression, canBeSingle: false)
            : this;
    }

    public override FieldsProjectionExpression ToFieldsProjection()
    {
        return new FieldsProjectionExpression(
            Type,
            [new FieldProjectionExpression(Type, Expression)],
            CanBeSingle
        );
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        visitor.Visit(Expression);
        return this;
    }
}

internal sealed class FieldsProjectionExpression : ProjectionExpression
{
    public ImmutableArray<FieldProjectionExpression> Fields { get; }
    public override Expression? InnerExpression => Fields[0].Expression;

    public FieldsProjectionExpression(
        Type resultType,
        ImmutableArray<FieldProjectionExpression> fields,
        bool canBeSingle = true
    )
        : base(resultType, canBeSingle && fields.Length == 1)
    {
        Fields = fields;
    }

    public override ProjectionExpression TrySingle()
    {
        return CanBeSingle ? this : new FieldsProjectionExpression(Type, Fields, canBeSingle: true);
    }

    public override ProjectionExpression Unsingle()
    {
        return CanBeSingle
            ? new FieldsProjectionExpression(Type, Fields, canBeSingle: false)
            : this;
    }

    public override FieldsProjectionExpression ToFieldsProjection()
    {
        return this;
    }

    public ProjectionExpression Merge(FieldsProjectionExpression fieldsProjection)
    {
        return new FieldsProjectionExpression(
            Type,
            [.. Fields, .. fieldsProjection.Fields],
            CanBeSingle && fieldsProjection.CanBeSingle
        );
    }
}

internal sealed class CountFieldProjectionExpression : ProjectionExpression
{
    public LambdaExpression? Predicate { get; }
    public override Expression? InnerExpression => Predicate;

    public CountFieldProjectionExpression(
        Type resultType,
        LambdaExpression? predicate,
        bool canBeSingle = false
    )
        : base(resultType, canBeSingle)
    {
        Predicate = predicate;
    }

    public override ProjectionExpression TrySingle()
    {
        return CanBeSingle
            ? this
            : new CountFieldProjectionExpression(Type, Predicate, canBeSingle: true);
    }

    public override ProjectionExpression Unsingle()
    {
        return CanBeSingle
            ? new CountFieldProjectionExpression(Type, Predicate, canBeSingle: false)
            : this;
    }

    public override FieldsProjectionExpression ToFieldsProjection()
    {
        throw new NotSupportedException();
    }
}

internal sealed class AggregationFieldProjectionExpression : ProjectionExpression
{
    public AggregationType AggregationType { get; }
    public Expression Selector { get; }
    public string Alias { get; }

    public override Expression? InnerExpression => Selector;

    public AggregationFieldProjectionExpression(
        Type resultType,
        AggregationType aggregationType,
        Expression selector,
        string alias,
        bool canBeSingle = false
    )
        : base(resultType, canBeSingle)
    {
        AggregationType = aggregationType;
        Selector = selector;
        Alias = alias;
    }

    public override ProjectionExpression TrySingle()
    {
        return CanBeSingle
            ? this
            : new AggregationFieldProjectionExpression(
                Type,
                AggregationType,
                Selector,
                Alias,
                canBeSingle: true
            );
    }

    public override ProjectionExpression Unsingle()
    {
        return CanBeSingle
            ? new AggregationFieldProjectionExpression(
                Type,
                AggregationType,
                Selector,
                Alias,
                canBeSingle: false
            )
            : this;
    }

    public override FieldsProjectionExpression ToFieldsProjection()
    {
        throw new NotSupportedException();
    }
}

// /// <summary>
// ///
// /// </summary>
// internal sealed class SelfProjectionExpression : ProjectionExpression
// {
//     public ParameterExpression Expression { get; }
//     public override Expression? InnerExpression => Expression;
//
//     public SelfProjectionExpression(ParameterExpression expression, bool canBeSingle = false)
//         : base(expression.Type, canBeSingle)
//     {
//         Expression = expression;
//     }
//
//     public override ProjectionExpression Unsingle()
//     {
//         throw new NotImplementedException();
//     }
// }
