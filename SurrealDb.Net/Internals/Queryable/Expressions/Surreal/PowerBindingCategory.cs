namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal enum PowerBindingCategory
{
    Or = 1,
    And,
    Equality,
    Relation,
    AddSub,
    MulDiv,
    Power,
    Cast,
    Range,
    Nullish,
    Unary,
}
