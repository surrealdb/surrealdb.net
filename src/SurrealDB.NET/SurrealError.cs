using System.Text.Json.Serialization;

namespace SurrealDB.NET;

public readonly record struct SurrealError(
    [property: JsonPropertyName("message")]
    string Message,

    [property: JsonPropertyName("code")]
    int Code);
