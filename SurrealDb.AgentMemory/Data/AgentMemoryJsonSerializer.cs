using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.AgentMemory.Client;

/// <summary>
/// The serializer calls the generated converters make, routed through the metadata their options carry.
/// </summary>
/// <remarks>
/// The generator emits <c>JsonSerializer.Deserialize&lt;T&gt;(ref reader, options)</c> and
/// <c>JsonSerializer.Serialize(writer, value, options)</c>, which trimming and NativeAOT flag because they can fall
/// back to reflection. <c>generate.sh</c> rewrites those calls to these, which resolve the
/// <see cref="JsonTypeInfo{T}"/> from the options: the source-generated <see cref="AgentMemoryJsonContext"/> when the
/// client reads or writes a model. The behaviour is the same, and an application that references this package
/// gets no trim or AOT warnings from it.
/// </remarks>
internal static class AgentMemoryJsonSerializer
{
    public static T? Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(ref reader, GetTypeInfo<T>(options));
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(json, GetTypeInfo<T>(options));
    }

    public static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, GetTypeInfo<T>(options));
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions options)
    {
        // Options that were never used have no resolver yet. The serializer would give them its
        // reflection-based default on first use; they get the source-generated context instead, which
        // knows every model and needs no reflection. Converters the caller added still come first.
        if (options.TypeInfoResolver is null && !options.IsReadOnly)
        {
            options.TypeInfoResolver = AgentMemoryJsonContext.Default;
        }

        return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
    }
}
