namespace SurrealDb.Net.Models;

public partial class RecordId
{
    /// <summary>
    /// Creates a new record ID from a generically typed table and a generically typed id.
    /// </summary>
    /// <typeparam name="TId">The type of the record id part</typeparam>
    /// <param name="table">Table name</param>
    /// <param name="id">Table id</param>
    /// <exception cref="ArgumentException"></exception>
    public static RecordId From<TId>(string table, TId id)
    {
        if (table is null)
            throw new ArgumentException("Table should not be null", nameof(table));

        if (id is null)
            throw new ArgumentException("Id should not be null", nameof(id));

        if (id is string idAsString)
            return new RecordIdOfString(table, idAsString);

        return new RecordIdOf<TId>(table, id);
    }
}
