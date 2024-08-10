namespace SurrealDb.Net.Models;

/// <summary>
/// Reflects a record ID (that contains both the record's table name and id).
/// Inherited implementation of <see cref="RecordIdOf{TId}"/>
/// that enforces the generic type of <see cref="RecordIdOf{TId}.Id"/> to be of type <see cref="string"/>.
/// </summary>
/// <remarks>
/// Example: `table_name:record_id`
/// </remarks>
public class RecordIdOfString : RecordIdOf<string>
{
    /// <summary>
    /// Creates a <see cref="RecordId"/> with defined table name and id of type <see cref="string"/>.
    /// </summary>
    /// <param name="table">Table part of the record id.</param>
    /// <param name="id">Id part of the record id.</param>
    public RecordIdOfString(string table, string id)
        : base(table, id) { }
}
