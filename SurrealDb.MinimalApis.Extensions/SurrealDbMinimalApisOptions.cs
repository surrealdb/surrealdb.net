﻿using Microsoft.AspNetCore.Builder;

namespace SurrealDb.MinimalApis.Extensions;

/// <summary>
/// A set of options to customize configuration of generated Minimal Api endpoints using <see cref="SurrealDbMinimalApisExtensions.MapSurrealEndpoints"/>.
/// </summary>
public class SurrealDbMinimalApisOptions
{
    /// <summary>
    /// Enable or disable all HTTP GET requests at once (endpoints to select all records or a single from a table).
    /// </summary>
    public bool? EnableQueries { get; set; }

    /// <summary>
    /// Enable or disable HTTP GET request to select all records from a SurrealDB table.
    /// </summary>
    public bool? EnableGetAll { get; set; }

    /// <summary>
    /// Enable or disable HTTP GET request to select a single record from a SurrealDB table.
    /// </summary>
    public bool? EnableGetSingle { get; set; }

    /// <summary>
    /// Enable or disable all HTTP requests that mutates data at once (POST, PUT, PATCH, DELETE).
    /// </summary>
    public bool? EnableMutations { get; set; }

    /// <summary>
    /// Enable or disable HTTP POST request to create a new record in a SurrealDB table.
    /// </summary>
    public bool? EnablePost { get; set; }

    /// <summary>
    /// Enable or disable HTTP PUT request to update a record in a SurrealDB table.
    /// </summary>
    public bool? EnablePut { get; set; }

    /// <summary>
    /// Enable or disable HTTP PATCH request to patch all records in a SurrealDB table.
    /// </summary>
    public bool? EnablePatchAll { get; set; }

    /// <summary>
    /// Enable or disable HTTP PATCH request to patch a single record in a SurrealDB table.
    /// </summary>
    public bool? EnablePatchSingle { get; set; }

    /// <summary>
    /// Enable or disable HTTP DELETE request to delete all records from a SurrealDB table.
    /// </summary>
    public bool? EnableDeleteAll { get; set; }

    /// <summary>
    /// Enable or disable HTTP DELETE request to delete a single record from a SurrealDB table.
    /// </summary>
    public bool? EnableDeleteSingle { get; set; }

    /// <summary>
    /// A set of tags to be used to document the generated endpoints using OpenAPI.
    /// </summary>
    public string[]? Tags { get; set; }
}
