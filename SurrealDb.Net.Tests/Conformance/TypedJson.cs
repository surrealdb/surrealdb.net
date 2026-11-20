using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;
using Microsoft.Spatial;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Tests.Conformance;

/// <summary>
/// The specification's typed JSON (spec/README.md, "Typed JSON"): picks the .NET type the SDK
/// decodes each value to, and puts both the vector's value and a decoded .NET value into one
/// normal form so they can be compared.
/// </summary>
/// <remarks>
/// The normal form spells every scalar as a single-key object holding a string
/// (<c>{"$int": "5"}</c>, <c>{"$float": "1.5"}</c>), every object as <c>{"$object": {...}}</c>,
/// and sorts set elements, so equal values give equal JSON.
/// </remarks>
public static class TypedJson
{
    private static readonly string[] SIZED_KINDS =
    [
        "$i8",
        "$u8",
        "$i16",
        "$u16",
        "$i32",
        "$u32",
        "$u64",
        "$f16",
        "$f32",
    ];

    /// <summary>
    /// The .NET type the SDK decodes <paramref name="value"/> to.
    /// </summary>
    /// <exception cref="NotSupportedException">The SDK has no type for the value.</exception>
    public static Type GetClrType(JsonNode? value)
    {
        switch (value)
        {
            case null:
                return typeof(object);
            case JsonArray array:
                return typeof(List<>).MakeGenericType(GetElementType(array));
            case JsonValue scalar:
                return scalar.GetValue<object>() switch
                {
                    bool => typeof(bool),
                    long => typeof(long),
                    string => typeof(string),
                    _ => throw new NotSupportedException("A float is written as {$float: ...}"),
                };
        }

        var obj = (JsonObject)value;
        if (!IsTyped(obj, out var key, out var inner))
        {
            return typeof(Dictionary<,>).MakeGenericType(
                typeof(string),
                GetElementType(obj.Select(x => x.Value))
            );
        }

        return key switch
        {
            "$none" => typeof(None),
            "$int" => typeof(long),
            "$float" => typeof(double),
            _ when SIZED_KINDS.Contains(key) => typeof(SizedNumber),
            "$decimal" => typeof(decimal),
            "$string" => typeof(string),
            "$bytes" => typeof(byte[]),
            "$uuid" => typeof(Guid),
            "$datetime" => typeof(DateTime),
            "$duration" => typeof(Duration),
            "$array" => GetClrType(inner),
            "$object" => typeof(Dictionary<,>).MakeGenericType(
                typeof(string),
                GetElementType(inner!.AsObject().Select(x => x.Value))
            ),
            "$set" => typeof(HashSet<>).MakeGenericType(GetElementType(inner!.AsArray())),
            "$record" => typeof(RecordId),
            "$range" => GetRangeType(inner!.AsObject()),
            "$file" => typeof(SurrealFile),
            "$geometry" => inner!["type"]!.GetValue<string>() switch
            {
                "Point" => typeof(GeometryPoint),
                "LineString" => typeof(GeometryLineString),
                "Polygon" => typeof(GeometryPolygon),
                "MultiPoint" => typeof(GeometryMultiPoint),
                "MultiLineString" => typeof(GeometryMultiLineString),
                "MultiPolygon" => typeof(GeometryMultiPolygon),
                "GeometryCollection" => typeof(GeometryCollection),
                var type => throw new NotSupportedException($"Unknown geometry {type}"),
            },
            "$table" => throw new NotSupportedException("The SDK has no table type"),
            _ => throw new NotSupportedException($"Unknown typed value {key}"),
        };
    }

    /// <summary>
    /// The normal form of a vector's typed JSON value.
    /// </summary>
    public static JsonNode? Normalize(JsonNode? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonArray array:
                return new JsonArray(array.Select(Normalize).ToArray());
            case JsonValue scalar:
                return scalar.GetValue<object>() switch
                {
                    bool b => JsonValue.Create(b),
                    long l => Typed("$int", l.ToString(CultureInfo.InvariantCulture)),
                    string s => JsonValue.Create(s),
                    var other => throw new NotSupportedException($"Untyped {other}"),
                };
        }

        var obj = (JsonObject)value;
        if (!IsTyped(obj, out var key, out var inner))
        {
            return NormalizeObject(obj);
        }

        return key switch
        {
            "$none" => Typed("$none", null),
            "$int" => Typed("$int", ScalarText(inner)),
            "$float" => Typed("$float", FloatText(inner)),
            "$f16" or "$f32" => Typed(key, FloatText(inner)),
            _ when SIZED_KINDS.Contains(key) => Typed(key, ScalarText(inner)),
            "$decimal" => Typed("$decimal", NormalizeDecimal(inner!.GetValue<string>())),
            "$string" => JsonValue.Create(inner!.GetValue<string>()),
            "$bytes" => Typed("$bytes", inner!.GetValue<string>().ToLowerInvariant()),
            "$uuid" => Typed("$uuid", inner!.GetValue<string>().ToLowerInvariant()),
            "$datetime" => Typed("$datetime", inner!.GetValue<string>()),
            "$duration" => Typed(
                "$duration",
                new JsonArray(
                    JsonValue.Create(ScalarText(inner![0])),
                    JsonValue.Create(ScalarText(inner[1]))
                )
            ),
            "$array" => Normalize(inner),
            "$object" => NormalizeObject(inner!.AsObject()),
            "$set" => NormalizeSet(inner!.AsArray().Select(Normalize)),
            "$record" => Typed(
                "$record",
                new JsonObject
                {
                    ["table"] = inner!["table"]!.GetValue<string>(),
                    ["key"] = Normalize(inner["key"]),
                }
            ),
            "$range" => Typed(
                "$range",
                new JsonObject
                {
                    ["start"] = NormalizeBound(inner!["start"]),
                    ["end"] = NormalizeBound(inner["end"]),
                }
            ),
            "$file" => Typed(
                "$file",
                new JsonObject
                {
                    ["bucket"] = inner!["bucket"]!.GetValue<string>(),
                    ["key"] = inner["key"]!.GetValue<string>(),
                }
            ),
            "$geometry" => Typed("$geometry", NormalizeGeoJson(inner!)),
            _ => Typed(key, inner?.DeepClone()),
        };
    }

    /// <summary>
    /// The normal form of a decoded .NET value. <paramref name="expected"/> is the vector's value,
    /// which says what to decode a record id key as.
    /// </summary>
    public static JsonNode? FromClr(object? value, JsonNode? expected)
    {
        switch (value)
        {
            case null:
                return null;
            case None:
                return Typed("$none", null);
            case bool b:
                return JsonValue.Create(b);
            case long l:
                return Typed("$int", l.ToString(CultureInfo.InvariantCulture));
            case int i:
                return Typed("$int", i.ToString(CultureInfo.InvariantCulture));
            case double d:
                return Typed("$float", FloatText(d));
            case SizedNumber sized:
                return Typed(
                    "$" + sized.Kind.ToString().ToLowerInvariant(),
                    sized.IsFloat ? FloatText(sized.ToDouble())
                        : sized.Kind == SizedNumberKind.U64
                            ? sized.ToUInt64().ToString(CultureInfo.InvariantCulture)
                        : sized.ToInt64().ToString(CultureInfo.InvariantCulture)
                );
            case decimal m:
                return Typed(
                    "$decimal",
                    NormalizeDecimal(m.ToString(CultureInfo.InvariantCulture))
                );
            case string s:
                return JsonValue.Create(s);
            case byte[] bytes:
                return Typed("$bytes", Convert.ToHexString(bytes).ToLowerInvariant());
            case Guid guid:
                return Typed("$uuid", guid.ToString("D"));
            case DateTime dateTime:
                return Typed("$datetime", FormatDateTime(dateTime));
            case Duration duration:
                var (seconds, nanos) = ToSecondsAndNanos(duration);
                return Typed(
                    "$duration",
                    new JsonArray(
                        JsonValue.Create(seconds.ToString(CultureInfo.InvariantCulture)),
                        JsonValue.Create(nanos.ToString(CultureInfo.InvariantCulture))
                    )
                );
            case RecordId recordId
                when recordId.GetType().IsGenericType
                    && recordId.GetType().GetGenericTypeDefinition() == typeof(RecordIdOf<>):
                return Typed(
                    "$record",
                    new JsonObject
                    {
                        ["table"] = recordId.Table,
                        ["key"] = FromClr(
                            recordId.GetType().GetProperty("Id")!.GetValue(recordId),
                            Unwrap(expected, "$record")?["key"]
                        ),
                    }
                );
            case RecordId recordId:
                var expectedKey = Unwrap(expected, "$record")?["key"];
                var keyType = GetClrType(expectedKey);
                object? key;
                try
                {
                    key = typeof(RecordId)
                        .GetMethod(nameof(RecordId.DeserializeId))!
                        .MakeGenericMethod(keyType)
                        .Invoke(recordId, null);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw;
                }
                return Typed(
                    "$record",
                    new JsonObject
                    {
                        ["table"] = recordId.Table,
                        ["key"] = FromClr(key, expectedKey),
                    }
                );
            case SurrealFile file:
                return Typed(
                    "$file",
                    new JsonObject { ["bucket"] = file.Bucket, ["key"] = file.Path }
                );
            case Geometry geometry:
                return Typed("$geometry", GeometryToGeoJson(geometry));
            case IDictionary dictionary:
                var obj = new JsonObject();
                var expectedObject = Unwrap(expected, "$object") as JsonObject;
                foreach (DictionaryEntry entry in dictionary)
                {
                    var name = (string)entry.Key;
                    obj[name] = FromClr(entry.Value, expectedObject?[name]);
                }
                return NormalizeObject(obj, normalized: true);
        }

        var type = value.GetType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            return NormalizeSet(((IEnumerable)value).Cast<object?>().Select(x => FromClr(x, null)));
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Range<,>))
        {
            var expectedRange = Unwrap(expected, "$range");
            return Typed(
                "$range",
                new JsonObject
                {
                    ["start"] = BoundFromClr(
                        type.GetProperty("Start")!.GetValue(value),
                        expectedRange?["start"]
                    ),
                    ["end"] = BoundFromClr(
                        type.GetProperty("End")!.GetValue(value),
                        expectedRange?["end"]
                    ),
                }
            );
        }
        if (value is IEnumerable enumerable)
        {
            var expectedArray = Unwrap(expected, "$array") as JsonArray;
            return new JsonArray(
                enumerable.Cast<object?>().Select((x, i) => FromClr(x, expectedArray?[i])).ToArray()
            );
        }

        throw new NotSupportedException($"No normal form for {type}");
    }

    private static Type GetElementType(IEnumerable<JsonNode?> elements)
    {
        var list = elements.ToList();
        var types = list.Where(x => x is not null).Select(GetClrType).Distinct().ToList();

        return types.Count switch
        {
            0 => typeof(object),
            1 when types[0].IsValueType && list.Any(x => x is null) =>
                typeof(Nullable<>).MakeGenericType(types[0]),
            1 => types[0],
            _ => throw new NotSupportedException(
                "The SDK has no type for a collection mixing "
                    + string.Join(", ", types.Select(x => x.Name))
            ),
        };
    }

    private static Type GetRangeType(JsonObject range)
    {
        static Type? BoundType(JsonNode? bound) =>
            bound is JsonObject obj ? GetClrType(obj.Single().Value) : null;

        var start = BoundType(range["start"]);
        var end = BoundType(range["end"]);

        return typeof(Range<,>).MakeGenericType(
            start ?? end ?? typeof(None),
            end ?? start ?? typeof(None)
        );
    }

    private static bool IsTyped(JsonObject obj, out string key, out JsonNode? inner)
    {
        if (obj.Count == 1)
        {
            var (name, value) = obj.Single();
            if (name.StartsWith('$'))
            {
                key = name;
                inner = value;
                return true;
            }
        }

        key = string.Empty;
        inner = null;
        return false;
    }

    private static JsonNode? Unwrap(JsonNode? value, string key)
    {
        return value is JsonObject obj && IsTyped(obj, out var name, out var inner) && name == key
            ? inner
            : value;
    }

    private static JsonObject Typed(string key, JsonNode? value)
    {
        return new JsonObject { [key] = value };
    }

    private static JsonObject NormalizeObject(JsonObject obj, bool normalized = false)
    {
        var sorted = new JsonObject();
        foreach (var (key, value) in obj.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            sorted[key] = normalized ? value?.DeepClone() : Normalize(value);
        }

        return Typed("$object", sorted);
    }

    private static JsonObject NormalizeSet(IEnumerable<JsonNode?> elements)
    {
        var sorted = elements
            .Select(x => x?.ToJsonString() ?? "null")
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(x => JsonNode.Parse(x))
            .ToArray();

        return Typed("$set", new JsonArray(sorted));
    }

    private static JsonNode? NormalizeBound(JsonNode? bound)
    {
        if (bound is not JsonObject obj)
        {
            return null;
        }

        var (key, value) = obj.Single();
        return new JsonObject { [key] = Normalize(value) };
    }

    private static JsonNode? BoundFromClr(object? bound, JsonNode? expected)
    {
        if (bound is null)
        {
            return null;
        }

        var type = bound.GetType();
        var boundType = (RangeBoundType)type.GetProperty("Type")!.GetValue(bound)!;
        var value = type.GetProperty("Value")!.GetValue(bound);
        string key = boundType == RangeBoundType.Inclusive ? "included" : "excluded";

        return new JsonObject { [key] = FromClr(value, expected?[key]) };
    }

    private static JsonNode NormalizeGeoJson(JsonNode geometry)
    {
        var obj = geometry.AsObject();
        var type = obj["type"]!.GetValue<string>();

        if (type == "GeometryCollection")
        {
            return new JsonObject
            {
                ["type"] = type,
                ["geometries"] = new JsonArray(
                    obj["geometries"]!.AsArray().Select(x => NormalizeGeoJson(x!)).ToArray()
                ),
            };
        }

        return new JsonObject
        {
            ["type"] = type,
            ["coordinates"] = NormalizeCoordinates(obj["coordinates"]!),
        };
    }

    private static JsonNode NormalizeCoordinates(JsonNode node)
    {
        return node is JsonArray array
            ? new JsonArray(array.Select(x => NormalizeCoordinates(x!)).ToArray())
            : JsonValue.Create(FloatText(node))!;
    }

    private static JsonNode GeometryToGeoJson(Geometry geometry)
    {
        static JsonArray Point(GeometryPoint point) =>
            new(JsonValue.Create(FloatText(point.X)), JsonValue.Create(FloatText(point.Y)));
        static JsonArray Line(GeometryLineString line) =>
            new(line.Points.Select(x => (JsonNode)Point(x)).ToArray());
        static JsonArray Polygon(GeometryPolygon polygon) =>
            new(polygon.Rings.Select(x => (JsonNode)Line(x)).ToArray());

        return geometry switch
        {
            GeometryPoint point => Shape("Point", Point(point)),
            GeometryLineString line => Shape("LineString", Line(line)),
            GeometryPolygon polygon => Shape("Polygon", Polygon(polygon)),
            GeometryMultiPoint points => Shape(
                "MultiPoint",
                new JsonArray(points.Points.Select(x => (JsonNode)Point(x)).ToArray())
            ),
            GeometryMultiLineString lines => Shape(
                "MultiLineString",
                new JsonArray(lines.LineStrings.Select(x => (JsonNode)Line(x)).ToArray())
            ),
            GeometryMultiPolygon polygons => Shape(
                "MultiPolygon",
                new JsonArray(polygons.Polygons.Select(x => (JsonNode)Polygon(x)).ToArray())
            ),
            GeometryCollection collection => new JsonObject
            {
                ["type"] = "GeometryCollection",
                ["geometries"] = new JsonArray(
                    collection.Geometries.Select(GeometryToGeoJson).ToArray()
                ),
            },
            _ => throw new NotSupportedException($"No GeoJSON for {geometry.GetType()}"),
        };

        static JsonObject Shape(string type, JsonArray coordinates) =>
            new() { ["type"] = type, ["coordinates"] = coordinates };
    }

    private static string ScalarText(JsonNode? node)
    {
        return node!.GetValue<object>() switch
        {
            long l => l.ToString(CultureInfo.InvariantCulture),
            string s => BigInteger.Parse(s, CultureInfo.InvariantCulture).ToString(),
            var other => throw new NotSupportedException($"Not an integer: {other}"),
        };
    }

    private static string FloatText(JsonNode? node)
    {
        return node!.GetValue<object>() switch
        {
            long l => FloatText(l),
            double d => FloatText(d),
            string s => s switch
            {
                "NaN" => "NaN",
                "Infinity" => "Infinity",
                "-Infinity" => "-Infinity",
                "-0" => "-0",
                _ => FloatText(double.Parse(s, CultureInfo.InvariantCulture)),
            },
            var other => throw new NotSupportedException($"Not a float: {other}"),
        };
    }

    private static string FloatText(double value)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }
        if (double.IsPositiveInfinity(value))
        {
            return "Infinity";
        }
        if (double.IsNegativeInfinity(value))
        {
            return "-Infinity";
        }
        if (value == 0 && double.IsNegative(value))
        {
            return "-0";
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string NormalizeDecimal(string value)
    {
        if (!value.Contains('.'))
        {
            return value == "-0" ? "0" : value;
        }

        var trimmed = value.TrimEnd('0').TrimEnd('.');
        return trimmed is "-0" or "" ? "0" : trimmed;
    }

    private static string FormatDateTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        long fractionTicks = utc.Ticks % TimeSpan.TicksPerSecond;
        string seconds = utc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

        return fractionTicks == 0
            ? seconds + "Z"
            : $"{seconds}.{(fractionTicks * 100).ToString("D9", CultureInfo.InvariantCulture)}Z";
    }

    private static (long Seconds, int Nanos) ToSecondsAndNanos(Duration duration)
    {
        long seconds =
            duration.Seconds
            + 60L * duration.Minutes
            + 3600L * duration.Hours
            + 86400L * duration.Days
            + 604800L * duration.Weeks
            + 31536000L * duration.Years;
        int nanos =
            duration.NanoSeconds + 1000 * duration.MicroSeconds + 1000000 * duration.MilliSeconds;

        return (seconds, nanos);
    }
}
