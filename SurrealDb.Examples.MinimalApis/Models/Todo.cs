using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Json;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.MinimalApis.Models;

public class Todo : IRecord
{
    internal const string Table = "todo";

    [RecordIdJsonConverter(Table)]
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("dueBy")]
    public DateOnly? DueBy { get; set; } = null;

    [Column("isComplete")]
    public bool IsComplete { get; set; } = false;
}
