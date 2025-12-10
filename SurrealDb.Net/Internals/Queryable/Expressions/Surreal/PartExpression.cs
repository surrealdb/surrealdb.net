using System.Collections.Immutable;
using System.Text;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal abstract class PartExpression : SurrealExpression;

internal sealed class AllPartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("[*]");
    }
}

internal sealed class FlattenPartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("...");
    }
}

internal sealed class LastPartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("[$]");
    }
}

internal sealed class FirstPartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("[0]");
    }
}

internal sealed class FieldPartExpression : PartExpression, IPrintableExpression
{
    public string FieldName { get; }

    public FieldPartExpression(string fieldName)
    {
        FieldName = fieldName;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('.');
        stringBuilder.Append(FieldName);
    }
}

internal sealed class IndexPartExpression : PartExpression, IPrintableExpression
{
    private readonly int _index;

    public IndexPartExpression(int index)
    {
        _index = index;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('[');
        stringBuilder.Append(_index);
        stringBuilder.Append(']');
    }
}

internal sealed class WherePartExpression : PartExpression
{
    public ValueExpression Value { get; }

    public WherePartExpression(ValueExpression value)
    {
        Value = value;
    }
}

internal sealed class GraphPartExpression : PartExpression
{
    // TODO
    private GraphPartExpression()
    {
        throw new NotImplementedException();
    }
}

internal sealed class ValuePartExpression : PartExpression
{
    public ValueExpression Value { get; }

    public ValuePartExpression(ValueExpression value)
    {
        Value = value;
    }
}

internal sealed class StartPartExpression : PartExpression
{
    public ValueExpression Value { get; }

    public StartPartExpression(ValueExpression value)
    {
        Value = value;
    }
}

internal sealed class MethodPartExpression : PartExpression
{
    public string Name { get; }
    public ImmutableArray<ValueExpression> Args { get; }

    public MethodPartExpression(string name)
    {
        Name = name;
        Args = [];
    }

    public MethodPartExpression(string name, ValueExpression[] args)
    {
        Name = name;
        Args = [.. args];
    }
}

internal sealed class DestructurePartExpression : PartExpression
{
    // TODO
    private DestructurePartExpression()
    {
        throw new NotImplementedException();
    }
}

internal sealed class OptionalPartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('?');
    }
}

internal sealed class RecursePartExpression : PartExpression
{
    // TODO
    private RecursePartExpression()
    {
        throw new NotImplementedException();
    }
}

internal sealed class DocPartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('@');
    }
}

internal sealed class RepeatRecursePartExpression : PartExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('.');
        stringBuilder.Append('@');
    }
}
