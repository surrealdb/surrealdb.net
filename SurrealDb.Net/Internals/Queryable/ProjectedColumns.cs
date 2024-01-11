using System.Collections.ObjectModel;
using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Expressions;

namespace SurrealDb.Net.Internals.Queryable;

internal sealed class ProjectedColumns
{
    public Expression Projector { get; }
    public ReadOnlyCollection<ColumnDeclaration> Columns { get; }

    internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
    {
        Projector = projector;
        Columns = columns;
    }
}
