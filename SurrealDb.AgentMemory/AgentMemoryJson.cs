using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.AgentMemory.Client;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Default <see cref="JsonSerializerOptions"/> with every converter the OpenAPI generator
/// emitted (<c>XxxJsonConverter</c> in the <c>Model</c> and <c>Client</c> namespaces) registered.
/// The generator does not attach these converters via attributes, so without them several
/// models (e.g. <c>CitationJson</c>) cannot be deserialized by System.Text.Json.
/// </summary>
public static class AgentMemoryJson
{
    private static readonly Lazy<JsonSerializerOptions> Default = new(CreateDefault);

    /// <summary>
    /// Shared default options. Treated as immutable: never modify after first use.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => Default.Value;

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions();

        foreach (var type in typeof(AgentMemoryJson).Assembly.GetTypes())
        {
            if (
                type.IsAbstract
                || !typeof(JsonConverter).IsAssignableFrom(type)
                || type.GetConstructor(Type.EmptyTypes) is null
                || type.Namespace
                    is not ("SurrealDb.AgentMemory.Model" or "SurrealDb.AgentMemory.Client")
            )
            {
                continue;
            }

            options.Converters.Add((JsonConverter)Activator.CreateInstance(type)!);
        }

        return options;
    }
}
