using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Builds a JSON request body the way the JavaScript client does:
/// a key is written only when the caller supplied a value, so absent options stay absent
/// and the server applies its own defaults.
/// </summary>
internal sealed class JsonBody
{
    /// <summary>
    /// Serializer options for request bodies: compact output, without escaping non-ASCII text,
    /// so a body (and the idempotency key derived from it) matches what <c>JSON.stringify</c> produces.
    /// </summary>
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    private readonly JsonObject _object = new();

    public JsonBody Add(string key, string? value)
    {
        if (value is not null)
        {
            _object[key] = value;
        }
        return this;
    }

    public JsonBody Add(string key, bool? value)
    {
        if (value is not null)
        {
            _object[key] = value.Value;
        }
        return this;
    }

    public JsonBody Add(string key, int? value)
    {
        if (value is not null)
        {
            _object[key] = value.Value;
        }
        return this;
    }

    public JsonBody Add(string key, long? value)
    {
        if (value is not null)
        {
            _object[key] = value.Value;
        }
        return this;
    }

    public JsonBody Add(string key, double? value)
    {
        if (value is not null)
        {
            _object[key] = value.Value;
        }
        return this;
    }

    /// <summary>
    /// Writes <c>true</c> only when the flag is set, as the JavaScript client does for opt-in switches.
    /// </summary>
    public JsonBody AddFlag(string key, bool? flag)
    {
        if (flag == true)
        {
            _object[key] = true;
        }
        return this;
    }

    public JsonBody Add(string key, IEnumerable<string>? values)
    {
        if (values is not null)
        {
            var array = new JsonArray();
            foreach (var value in values)
            {
                array.Add((JsonNode?)value);
            }
            _object[key] = array;
        }
        return this;
    }

    /// <summary>
    /// Writes a scope selector in its normalized wire shape, omitting one that normalizes to nothing.
    /// </summary>
    public JsonBody Add(string key, Scope? scope)
    {
        var normalized = Scope.Normalize(scope);
        if (normalized is not null)
        {
            _object[key] = ToJson(normalized);
        }
        return this;
    }

    public JsonBody Add(string key, JsonNode? node)
    {
        if (node is not null)
        {
            _object[key] = node.DeepClone();
        }
        return this;
    }

    public JsonBody Add<T>(string key, T? value, JsonTypeInfo<T> typeInfo)
        where T : class
    {
        if (value is not null)
        {
            _object[key] = JsonSerializer.SerializeToNode(value, typeInfo);
        }
        return this;
    }

    public JsonBody AddEach<T>(string key, IEnumerable<T>? values, JsonTypeInfo<T> typeInfo)
    {
        if (values is not null)
        {
            var array = new JsonArray();
            foreach (var value in values)
            {
                array.Add(JsonSerializer.SerializeToNode(value, typeInfo));
            }
            _object[key] = array;
        }
        return this;
    }

    public JsonBody Add(string key, IReadOnlyDictionary<string, IEnumerable<string>>? map)
    {
        if (map is not null)
        {
            var value = new JsonObject();
            foreach (var (name, items) in map)
            {
                var array = new JsonArray();
                foreach (var item in items)
                {
                    array.Add((JsonNode?)item);
                }
                value[name] = array;
            }
            _object[key] = value;
        }
        return this;
    }

    public bool IsEmpty => _object.Count == 0;

    public JsonObject Build()
    {
        return _object;
    }

    internal static JsonArray ToJson(IReadOnlyList<IReadOnlyList<string>> clauses)
    {
        var outer = new JsonArray();
        foreach (var clause in clauses)
        {
            var inner = new JsonArray();
            foreach (var path in clause)
            {
                inner.Add((JsonNode?)path);
            }
            outer.Add((JsonNode)inner);
        }
        return outer;
    }
}
