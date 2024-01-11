using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class WhatExpression : SurrealExpression
{
    public ImmutableArray<ValueExpression> Values { get; private set; }

    private WhatExpression() { }

    public static WhatExpression From(ValueExpression value)
    {
        return new WhatExpression { Values = [value] };
    }
    // public static WhatExpression From(Intermediate.SourceExpression sourceExpression)
    // {
    //     if (sourceExpression is Intermediate.TableSourceExpression tableSourceExpression)
    //     {
    //         return new WhatExpression
    //         {
    //             Values = [new TableValueExpression(tableSourceExpression.Table.TableName)],
    //         };
    //     }
    //     if (sourceExpression is Intermediate.SelectSourceExpression selectSourceExpression)
    //     {
    //         return new WhatExpression
    //         {
    //             Values = [new SubqueryValueExpression(selectSourceExpression.Table.TableName)],
    //         };
    //     }
    //
    //     throw new InvalidCastException(
    //         $"Failed to convert source expression of type '{sourceExpression.Type.Name}' to WhatExpression."
    //     );
    // }
}
