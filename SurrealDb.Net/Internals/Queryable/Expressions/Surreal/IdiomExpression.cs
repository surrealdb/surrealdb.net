using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class IdiomExpression : SurrealExpression
{
    public ImmutableArray<PartExpression> Parts { get; }

    public bool IsSingleFieldPart => Parts is [FieldPartExpression];

    public IdiomExpression(PartExpression[] parts)
    {
        Parts = [.. parts];
    }

    public static IdiomExpression Chain(PartExpression left, PartExpression right)
    {
        return new IdiomExpression([left, right]);
    }

    public static IdiomExpression Chain(IdiomExpression left, PartExpression right)
    {
        return new IdiomExpression([.. left.Parts, right]);
    }

    public static IdiomExpression Chain(IdiomExpression left, IdiomExpression right)
    {
        return new IdiomExpression([.. left.Parts, .. right.Parts]);
    }

    public bool IsSame(IdiomExpression other)
    {
        // 💡 Avoid redefining "Equality" for sanity
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Parts.Length != other.Parts.Length)
        {
            return false;
        }

        for (var index = 0; index < Parts.Length; index++)
        {
            var leftPart = Parts[index];
            var rightPart = other.Parts[index];

            if (
                leftPart is FieldPartExpression leftFieldPart
                && rightPart is FieldPartExpression rightFieldPart
                && leftFieldPart.FieldName == rightFieldPart.FieldName
            )
            {
                continue;
            }

            if (leftPart != rightPart)
            {
                return false;
            }
        }

        return true;
    }
}
