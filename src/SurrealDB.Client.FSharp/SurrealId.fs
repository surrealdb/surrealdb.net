namespace SurrealDB.Client.FSharp

open System
open System.Text.Json.Serialization
open System.Text.RegularExpressions

/// <summary>
/// Represents a SurrealDB Entity ID.
/// </summary>
[<Struct>]
type SurrealId =
    { table: string
      id: string }

    /// <summary>
    /// Tries to create a new SurrealId.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="id">The ID.</param>
    /// <returns>A new SurrealId, or an error message.</returns>
    static member TryCreate(table, id) =
        if String.IsNullOrWhiteSpace(table) then
            Error EMPTY_TABLE_NAME
        elif String.IsNullOrWhiteSpace(id) then
            Error EMPTY_ID
        elif Regex.IsMatch(table, @"^[^:]+$") |> not then
            Error INVALID_TABLE_NAME
        else
            Ok { table = table; id = id }

    /// <summary>
    /// Creates a new SurrealId.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="id">The ID.</param>
    /// <returns>A new SurrealId.</returns>
    /// <exception cref="FormatException">Thrown if the table name or ID are invalid.</exception>
    static member Create(table, id) =
        match SurrealId.TryCreate(table, id) with
        | Ok id -> id
        | Error msg -> raise <| FormatException msg

    /// <summary>
    /// Returns a string that represents the surreal ID in the format "table:id".
    /// </summary>
    override this.ToString() = $"{this.table}:{this.id}"

    /// <summary>
    /// Try to parse a SurrealId from a string.
    /// </summary>
    /// <param name="identifier">The string to parse.</param>
    /// <returns>The parsed SurrealId, or an error message.</returns>
    static member TryParse(identifier: string) =
        let parts = identifier.Split(':', 2)

        if parts.Length = 2 then
            SurrealId.TryCreate(parts.[0], parts.[1])
        else
            Error MISSING_COLON

    /// <summary>
    /// Parse a SurrealId from a string.
    /// </summary>
    /// <exception cref="FormatException">Thrown if the string is not a valid SurrealId.</exception>
    static member Parse(identifier: string) =
        match SurrealId.TryParse(identifier) with
        | Ok id -> id
        | Error error -> raise <| FormatException error

type SurrealIdConverter() =
    inherit JsonConverter<SurrealId>()

    override this.Read(reader, typeToConvert, options) =
        let identifier = reader.GetString()
        SurrealId.Parse(identifier)

    override this.Write(writer, value, options) =
        writer.WriteStringValue(value.ToString())
