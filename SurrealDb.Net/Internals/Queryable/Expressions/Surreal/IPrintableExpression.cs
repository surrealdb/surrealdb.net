using System.Text;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal interface IPrintableExpression
{
    void AppendTo(StringBuilder stringBuilder);
}
