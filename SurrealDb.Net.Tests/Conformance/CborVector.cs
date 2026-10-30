using System.Globalization;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace SurrealDb.Net.Tests.Conformance;

/// <summary>
/// A value test vector from the SurrealDB protocol specification
/// (github.com/surrealdb/surrealdb-protocol, spec/vectors), fetched into
/// Conformance/surrealdb-protocol by Conformance/fetch-protocol-vectors.sh.
/// </summary>
public sealed class CborVector
{
    private const string VECTORS_DIRECTORY = "Conformance/surrealdb-protocol/spec/vectors";

    /// <summary>
    /// The <see cref="Kind"/> of the one vector <see cref="Load"/> returns when the vectors have
    /// not been fetched.
    /// </summary>
    public const string MISSING_KIND = "missing";

    public required string File { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string Cbor { get; init; }
    public string? Canonical { get; init; }
    public string? Diag { get; init; }
    public JsonNode? Value { get; init; }
    public IReadOnlyList<string>? Formats { get; init; }

    /// <summary>
    /// Whether the vector applies to a codec speaking <paramref name="format"/>
    /// (<c>cbor</c> or <c>cbor.2</c>). A vector without <c>formats</c> applies to both.
    /// </summary>
    public bool AppliesTo(string format)
    {
        return Formats is null || Formats.Contains(format);
    }

    public override string ToString()
    {
        return $"{File}:{Name}";
    }

    public static IReadOnlyList<CborVector> Load(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, VECTORS_DIRECTORY, file);

        if (!System.IO.File.Exists(path))
        {
            return
            [
                new CborVector
                {
                    File = file,
                    Name = "not-fetched",
                    Kind = MISSING_KIND,
                    Cbor = string.Empty,
                },
            ];
        }

        using var reader = new StreamReader(path);
        var stream = new YamlStream();
        stream.Load(reader);

        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var vectors = (YamlSequenceNode)root.Children[new YamlScalarNode("vectors")];

        return vectors
            .Children.Cast<YamlMappingNode>()
            .Select(node => new CborVector
            {
                File = file,
                Name = GetString(node, "name")!,
                Kind = GetString(node, "kind")!,
                Cbor = GetString(node, "cbor")!,
                Canonical = GetString(node, "canonical"),
                Diag = GetString(node, "diag"),
                Value = node.Children.TryGetValue(new YamlScalarNode("value"), out var value)
                    ? ToJson(value)
                    : null,
                Formats = node.Children.TryGetValue(new YamlScalarNode("formats"), out var formats)
                    ? ((YamlSequenceNode)formats)
                        .Children.Select(x => ((YamlScalarNode)x).Value!)
                        .ToList()
                    : null,
            })
            .ToList();
    }

    private static string? GetString(YamlMappingNode node, string key)
    {
        return node.Children.TryGetValue(new YamlScalarNode(key), out var value)
            ? ((YamlScalarNode)value).Value
            : null;
    }

    /// <summary>
    /// Converts YAML to JSON, keeping integers and floats apart as YAML's core schema does.
    /// </summary>
    private static JsonNode? ToJson(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                var obj = new JsonObject();
                foreach (var (key, value) in mapping.Children)
                {
                    obj[((YamlScalarNode)key).Value!] = ToJson(value);
                }
                return obj;
            case YamlSequenceNode sequence:
                var array = new JsonArray();
                foreach (var item in sequence.Children)
                {
                    array.Add(ToJson(item));
                }
                return array;
            case YamlScalarNode scalar:
                string text = scalar.Value ?? string.Empty;

                if (scalar.Style != ScalarStyle.Plain)
                {
                    return JsonValue.Create(text);
                }
                if (text is "null" or "~" or "")
                {
                    return null;
                }
                if (text is "true" or "false")
                {
                    return JsonValue.Create(text == "true");
                }
                if (
                    long.TryParse(
                        text,
                        NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out long integer
                    )
                )
                {
                    return JsonValue.Create(integer);
                }
                if (
                    double.TryParse(
                        text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number
                    )
                )
                {
                    return JsonValue.Create(number);
                }
                return JsonValue.Create(text);
            default:
                throw new NotSupportedException($"Unexpected YAML node {node.NodeType}");
        }
    }
}
