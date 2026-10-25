using System.Buffers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;
using Dahomey.Cbor;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Tests.Conformance;

/// <summary>
/// Runs the SurrealDB protocol's CBOR value vectors through the SDK's codec, as
/// spec/README.md ("Using the vectors from an SDK") describes:
/// <list type="bullet">
/// <item><c>canonical</c>: decode <c>cbor</c> to <c>value</c>, and encode that back to exactly <c>cbor</c>.</item>
/// <item><c>decode</c>: decode <c>cbor</c> to <c>value</c>.</item>
/// <item><c>reject</c>: fail to decode <c>cbor</c>.</item>
/// </list>
/// The plain <c>cbor</c> codec runs cbor.yaml. The <c>cbor.2</c> codec runs cbor2.yaml, and every
/// cbor.yaml vector whose <c>formats</c> does not leave cbor.2 out.
/// </summary>
public class CborVectorTests
{
    private const string CBOR = "cbor";
    private const string CBOR2 = "cbor.2";

    private const string MIXED =
        "The SDK decodes into typed .NET models, and has no type for a collection that mixes value kinds. SizedNumberConverterTests covers the cbor.2 ones with typed models.";

    private const string NONE_AND_NULL =
        "A nullable None reads NONE (tag 6 around null) as null, so no .NET collection type keeps NONE and NULL apart.";

    private const string DATETIME_PRECISION =
        "DateTime holds 100-nanosecond ticks, so nanoseconds are rounded.";

    private const string PRE_EPOCH_DATETIME =
        "A DateTime before 1970 with a fraction of a second is encoded one second late: 12([0, 500000000]) instead of 12([-1, 500000000]).";

    private const string DURATION_SECONDS =
        "Duration holds its seconds as a signed 64-bit integer.";

    private const string UNBOUNDED_RANGE =
        "The range converter cannot read an unbounded (null) side.";

    private const string STRING_RECORD_ID =
        "A record id in string form (tag 8 around a string) is not decoded. Servers never send it.";

    private const string TABLE = "The SDK has no table type (tag 7).";

    private const string DATETIME_SHAPE =
        "The datetime converter does not check the [seconds, nanoseconds] array.";

    private const string DURATION_SIGN = "The duration converter accepts negative parts.";

    private const string DECIMAL_PARSE = "decimal parsing rounds or trims instead of failing.";

    private const string NONE_PAYLOAD = "The NONE converter ignores what tag 6 wraps.";

    private const string EMPTY_POLYGON = "The geometry converters accept an empty polygon.";

    private const string LAZY_RECORD_ID_KEY =
        "A RecordId keeps its key undecoded until DeserializeId, so decoding it does not check the key.";

    private const string UNKNOWN_TAG = "Unknown tags are skipped rather than refused.";

    /// <summary>
    /// Vectors the SDK does not pass yet, and why. Remove an entry once the SDK passes it.
    /// </summary>
    private static readonly Dictionary<string, string> KnownGaps = new()
    {
        ["cbor.yaml:array-indefinite-length"] = MIXED,
        ["cbor.yaml:array-mixed"] = MIXED,
        ["cbor.yaml:object-nested"] = MIXED,
        ["cbor.yaml:set-mixed"] = MIXED,
        ["cbor.yaml:record-id-array-key"] = MIXED,
        ["cbor.yaml:record-id-object-key"] = MIXED,
        ["cbor.yaml:sized-numbers-in-record-id-key-widen"] = MIXED,
        ["cbor2.yaml:int-stays-untagged"] = MIXED,
        ["cbor2.yaml:sized-in-array-and-object"] = MIXED,
        ["cbor.yaml:none-and-null-in-array"] = NONE_AND_NULL,
        ["cbor.yaml:none-and-null-in-object"] = NONE_AND_NULL,
        ["cbor.yaml:datetime-nanosecond-precision"] = DATETIME_PRECISION,
        ["cbor.yaml:datetime-one-nanosecond-after-epoch"] = DATETIME_PRECISION,
        ["cbor.yaml:datetime-pre-epoch-last-nanosecond"] = DATETIME_PRECISION,
        ["cbor.yaml:datetime-year-9999"] = DATETIME_PRECISION,
        ["cbor.yaml:datetime-pre-epoch-half-second"] = PRE_EPOCH_DATETIME,
        ["cbor.yaml:duration-u64-seconds"] = DURATION_SECONDS,
        ["cbor.yaml:range-unbounded"] = UNBOUNDED_RANGE,
        ["cbor.yaml:range-unbounded-start"] = UNBOUNDED_RANGE,
        ["cbor.yaml:range-unbounded-end"] = UNBOUNDED_RANGE,
        ["cbor.yaml:record-id-range-key-unbounded-start"] = UNBOUNDED_RANGE,
        ["cbor.yaml:record-id-string-form"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-array-key"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-backtick-table"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-digits-past-i64"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-escaped-table"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-escapes"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-int-key"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-object-key"] = STRING_RECORD_ID,
        ["cbor.yaml:record-id-string-form-uuid-key"] = STRING_RECORD_ID,
        ["cbor.yaml:table"] = TABLE,
        ["cbor.yaml:reject-datetime-negative-nanoseconds"] = DATETIME_SHAPE,
        ["cbor.yaml:reject-datetime-one-element"] = DATETIME_SHAPE,
        ["cbor.yaml:reject-duration-negative-nanoseconds"] = DURATION_SIGN,
        ["cbor.yaml:reject-duration-negative-seconds"] = DURATION_SIGN,
        ["cbor.yaml:reject-decimal-mantissa-beyond-96-bits"] = DECIMAL_PARSE,
        ["cbor.yaml:reject-decimal-scale-beyond-28"] = DECIMAL_PARSE,
        ["cbor.yaml:reject-decimal-trailing-whitespace"] = DECIMAL_PARSE,
        ["cbor.yaml:reject-none-with-payload"] = NONE_PAYLOAD,
        ["cbor.yaml:reject-polygon-empty"] = EMPTY_POLYGON,
        ["cbor.yaml:reject-record-id-bool-key"] = LAZY_RECORD_ID_KEY,
        ["cbor.yaml:reject-record-id-decimal-key"] = LAZY_RECORD_ID_KEY,
        ["cbor.yaml:reject-record-id-float-key"] = LAZY_RECORD_ID_KEY,
        ["cbor.yaml:reject-record-id-range-bool-bound"] = LAZY_RECORD_ID_KEY,
        ["cbor.yaml:reject-record-id-uuid-string-key"] = LAZY_RECORD_ID_KEY,
        ["cbor2.yaml:reject-f16-record-id-key"] = LAZY_RECORD_ID_KEY,
        ["cbor.yaml:reject-unknown-tag"] = UNKNOWN_TAG,
    };

    /// <summary>
    /// What to decode a reject vector as, when its first tag or major type does not say.
    /// </summary>
    private static readonly Dictionary<string, Type> RejectTypes = new();

    public static IEnumerable<Func<CborVector>> PlainCborVectors()
    {
        return CborVector
            .Load("cbor.yaml")
            .Where(x => x.AppliesTo(CBOR))
            .Select(x => (Func<CborVector>)(() => x));
    }

    public static IEnumerable<Func<CborVector>> Cbor2Vectors()
    {
        return CborVector
            .Load("cbor2.yaml")
            .Concat(CborVector.Load("cbor.yaml").Where(x => x.AppliesTo(CBOR2)))
            .Select(x => (Func<CborVector>)(() => x));
    }

    [Test]
    [MethodDataSource(nameof(PlainCborVectors))]
    public void ShouldPassPlainCborVector(CborVector vector)
    {
        Run(vector, CBOR, SurrealDbCborOptions.GetCborSerializerOptions(null, cbor2: false));
    }

    [Test]
    [MethodDataSource(nameof(Cbor2Vectors))]
    public void ShouldPassCbor2Vector(CborVector vector)
    {
        Run(vector, CBOR2, SurrealDbCborOptions.GetCborSerializerOptions(null, cbor2: true));
    }

    private static void Run(CborVector vector, string format, CborOptions options)
    {
        if (vector.Kind == CborVector.MISSING_KIND)
        {
            const string NOT_FETCHED =
                "The protocol vectors have not been fetched. Run Conformance/fetch-protocol-vectors.sh, then build again.";

            // 💡 CI must run the vectors, so a missing fetch fails there instead of skipping
            if (Environment.GetEnvironmentVariable("CI") == "true")
            {
                throw new InvalidOperationException(NOT_FETCHED);
            }

            Skip.Test(NOT_FETCHED);
        }

        if (
            KnownGaps.TryGetValue($"{format}:{vector}", out var reason)
            || KnownGaps.TryGetValue(vector.ToString(), out reason)
        )
        {
            Skip.Test(reason);
        }

        var bytes = Convert.FromHexString(vector.Cbor);

        switch (vector.Kind)
        {
            case "canonical":
            {
                var type = TypedJson.GetClrType(vector.Value);
                var decoded = Decode(bytes, type, options);
                AssertValue(decoded, vector.Value);

                var encoded = Encode(decoded, type, options);
                Convert
                    .ToHexString(encoded)
                    .ToLowerInvariant()
                    .Should()
                    .Be(vector.Cbor, "the SDK should encode {0} as the server does", vector.Diag);
                break;
            }
            case "decode":
            {
                var type = TypedJson.GetClrType(vector.Value);
                var decoded = Decode(bytes, type, options);
                AssertValue(decoded, vector.Value);
                break;
            }
            case "reject":
            {
                var type = GetRejectType(vector);
                Action decode = () => Decode(bytes, type, options);
                decode
                    .Should()
                    .Throw<Exception>(
                        "the server rejects {0}, so decoding it as {1} should fail",
                        vector.Diag,
                        type.Name
                    );
                break;
            }
            default:
                throw new NotSupportedException($"Unknown vector kind {vector.Kind}");
        }
    }

    private static object? Decode(byte[] bytes, Type type, CborOptions options)
    {
        try
        {
            return typeof(CborVectorTests)
                .GetMethod(nameof(DecodeAs), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [bytes, options]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static T DecodeAs<T>(byte[] bytes, CborOptions options)
    {
        return Dahomey.Cbor.Cbor.Deserialize<T>(bytes.AsSpan(), options);
    }

    private static byte[] Encode(object? value, Type type, CborOptions options)
    {
        return (byte[])
            typeof(CborVectorTests)
                .GetMethod(nameof(EncodeAs), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, [value, options])!;
    }

    private static byte[] EncodeAs<T>(T value, CborOptions options)
    {
        var writer = new ArrayBufferWriter<byte>();
        Dahomey.Cbor.Cbor.Serialize(value, writer, options);

        return writer.WrittenSpan.ToArray();
    }

    private static void AssertValue(object? decoded, JsonNode? expected)
    {
        var actualJson = TypedJson.FromClr(decoded, expected)?.ToJsonString() ?? "null";
        var expectedJson = TypedJson.Normalize(expected)?.ToJsonString() ?? "null";

        actualJson.Should().Be(expectedJson);
    }

    private static Type GetRejectType(CborVector vector)
    {
        if (RejectTypes.TryGetValue(vector.Name, out var type))
        {
            return type;
        }

        var bytes = Convert.FromHexString(vector.Cbor);
        int majorType = bytes[0] >> 5;

        if (majorType != 6)
        {
            return majorType switch
            {
                0 or 1 => typeof(long),
                2 => typeof(byte[]),
                3 => typeof(string),
                4 => typeof(List<long>),
                5 => typeof(Dictionary<string, long>),
                _ => typeof(double),
            };
        }

        ulong tag = (bytes[0] & 0x1f) switch
        {
            24 => bytes[1],
            25 => (ulong)(bytes[1] << 8 | bytes[2]),
            var small when small < 24 => (ulong)small,
            _ => ulong.MaxValue,
        };

        return tag switch
        {
            0 or 12 => typeof(DateTime),
            6 => typeof(None),
            8 => typeof(RecordId),
            9 or 37 => typeof(Guid),
            10 => typeof(decimal),
            13 or 14 => typeof(Duration),
            15 => typeof(Future),
            49 => typeof(Range<long, long>),
            55 => typeof(SurrealFile),
            56 => typeof(HashSet<long>),
            88 => typeof(Microsoft.Spatial.GeometryPoint),
            89 => typeof(Microsoft.Spatial.GeometryLineString),
            90 => typeof(Microsoft.Spatial.GeometryPolygon),
            91 => typeof(Microsoft.Spatial.GeometryMultiPoint),
            92 => typeof(Microsoft.Spatial.GeometryMultiLineString),
            93 => typeof(Microsoft.Spatial.GeometryMultiPolygon),
            94 => typeof(Microsoft.Spatial.GeometryCollection),
            >= 180 and <= 188 => typeof(SizedNumber),
            _ => typeof(object),
        };
    }
}
