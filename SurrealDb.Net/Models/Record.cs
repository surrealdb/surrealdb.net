using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Json;

namespace SurrealDb.Net.Models;

/// <summary>
/// The abstract class for Record type.
/// </summary>
public abstract class Record : IRecord
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }
}

/// <summary>
/// The interface for Record type.
/// </summary>
public interface IRecord
{
    /// <summary>
    /// The id of the record
    /// </summary>
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }
}
