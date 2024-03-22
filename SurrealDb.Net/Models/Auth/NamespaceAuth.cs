﻿using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// Credentials for the namespace user
/// </summary>
public sealed class NamespaceAuth
{
    /// <summary>
    /// The namespace the user has access to
    /// </summary>
    [JsonPropertyName("ns")]
    [CborProperty("ns")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The username of the namespace user
    /// </summary>
    [JsonPropertyName("user")]
    [CborProperty("user")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password of the namespace user
    /// </summary>
    [JsonPropertyName("pass")]
    [CborProperty("pass")]
    public string Password { get; set; } = string.Empty;
}
