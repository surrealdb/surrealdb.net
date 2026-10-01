using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory.Client;

/// <summary>
/// Selects the ResourceRef variant from its kind discriminator without probing other variants.
/// </summary>
public sealed class ResourceRefDiscriminatedJsonConverter : JsonConverter<ResourceRef>
{
    public override ResourceRef Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("A resource reference must be an object.");
        }

        var probe = reader;
        using var document = JsonDocument.ParseValue(ref probe);
        if (
            !document.RootElement.TryGetProperty("kind", out var kindElement)
            || kindElement.ValueKind != JsonValueKind.String
        )
        {
            throw new JsonException("A resource reference must have a string kind.");
        }

        return kindElement.GetString() switch
        {
            "entity" => new ResourceRef(Deserialize<ResourceRefOneOf>(ref reader, options)),
            "attribute" => new ResourceRef(Deserialize<ResourceRefOneOf1>(ref reader, options)),
            "relation" => new ResourceRef(Deserialize<ResourceRefOneOf2>(ref reader, options)),
            "action" => new ResourceRef(Deserialize<ResourceRefOneOf3>(ref reader, options)),
            "document" => new ResourceRef(Deserialize<ResourceRefOneOf4>(ref reader, options)),
            "session" => new ResourceRef(Deserialize<ResourceRefOneOf5>(ref reader, options)),
            var kind => throw new JsonException($"Unknown resource reference kind: '{kind}'."),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ResourceRef value,
        JsonSerializerOptions options
    )
    {
        if (value.ResourceRefOneOf is { } entity)
        {
            Serialize(writer, entity, options);
        }
        else if (value.ResourceRefOneOf1 is { } attribute)
        {
            Serialize(writer, attribute, options);
        }
        else if (value.ResourceRefOneOf2 is { } relation)
        {
            Serialize(writer, relation, options);
        }
        else if (value.ResourceRefOneOf3 is { } action)
        {
            Serialize(writer, action, options);
        }
        else if (value.ResourceRefOneOf4 is { } document)
        {
            Serialize(writer, document, options);
        }
        else if (value.ResourceRefOneOf5 is { } session)
        {
            Serialize(writer, session, options);
        }
        else
        {
            throw new JsonException("A resource reference must have a variant.");
        }
    }

    private static T Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        return JsonSerializer.Deserialize(ref reader, typeInfo)
            ?? throw new JsonException("A resource reference variant cannot be null.");
    }

    private static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        JsonSerializer.Serialize(writer, value, typeInfo);
    }
}
