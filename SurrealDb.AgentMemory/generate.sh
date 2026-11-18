#!/usr/bin/env sh
# Regenerates the models in Generated/ from spec/openapi.json.
#
# Requires openapi-generator-cli on the PATH (the npm package @openapitools/openapi-generator-cli,
# which needs Node.js and Java). The generator version is pinned in openapitools.json, next to this
# script; the CLI reads that file from the working directory, so the script changes into its own
# directory first.
#
# Only the models and the client utilities they depend on are kept. The generated operations
# client is not: AgentMemoryClient is hand-written, like the JavaScript and Python clients,
# and uses these models as its request and response types.
set -eu

cd "$(dirname "$0")"

# Byte-wise sorting, so the emitted serializer context is the same on every machine.
export LC_ALL=C

OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT

openapi-generator-cli generate \
    -g csharp \
    -i spec/openapi.json \
    -o "$OUT" \
    --additional-properties 'library=generichost,packageName=SurrealDb.AgentMemory,packageVersion=0.2.1,nullableReferenceTypes=true,netCoreProjectFile=true,optionalProjectFile=false,optionalAssemblyInfo=false'

rm -rf Generated
mkdir -p Generated/Client

cp -r "$OUT/src/SurrealDb.AgentMemory/Model" Generated/
# The generated DateTime converters are left out on purpose: they accept at most seven fractional
# digits, and the service writes nanosecond timestamps (nine digits). System.Text.Json's own
# DateTime parsing reads those; UtcDateTimeJsonConverter (Data/) wraps it, so a timestamp is UTC
# whichever offset it was written with, as it was with the generated converters.
for file in ClientUtils Option DateOnlyJsonConverter DateOnlyNullableJsonConverter; do
    cp "$OUT/src/SurrealDb.AgentMemory/Client/$file.cs" Generated/Client/
done

# The generator writes timestamps with ToString(format), which uses the current culture's calendar:
# under th-TH the year 2026 goes out as 2569, and the service stores it. Pin the invariant culture.
for f in $(grep -rlE '\.ToString\([A-Za-z]+Format\)' Generated/Model); do
    perl -pi -e 's/\.ToString\(([A-Za-z]+Format)\)/.ToString($1, System.Globalization.CultureInfo.InvariantCulture)/g' "$f"
done
perl -pi -e 's/\b(dateTime|dateTimeOffset|dateOnly)\.ToString\(format\)/$1.ToString(format, System.Globalization.CultureInfo.InvariantCulture)/g' Generated/Client/ClientUtils.cs

# The service's JSON encoder writes a non-finite float as null: the mean of no rows is NaN, and
# NaN has no JSON form. A required float would refuse that null and fail the whole response, so
# it is read back as NaN. Only properties the model marks as not nullable are touched; an
# optional one keeps its null.
for f in $(grep -rlE '\((double|float)\?\)null : ' Generated/Model); do
    perl -0pi -e '
        my %required = map { $_ => 1 }
            /if \((\w+)\.IsSet && \1\.Value == null\)\s*throw new ArgumentNullException\(nameof\(\1\), "Property is not nullable/g;
        s{^([ \t]*)(\w+) = new Option<(double|float)\?>\(utf8JsonReader\.TokenType == JsonTokenType\.Null \? \(\3\?\)null :}
         {$required{$2} ? "$1$2 = new Option<$3?>(utf8JsonReader.TokenType == JsonTokenType.Null ? ($3?)$3.NaN :" : $&}gme;
    ' "$f"
done

# Nested per-model enums are all named KindEnum; a single source-generation context
# collides on the shared simple name (SYSLIB1031). Rename each to a unique <Model>KindEnum
# (wire format is unaffected: JSON names come from the enum value mapping, not the type name).
# perl rather than sed: `sed -i` and `\b` differ between GNU and BSD sed.
for f in $(grep -rl 'enum KindEnum' Generated/Model); do
    name="$(basename "$f" .cs)"
    perl -pi -e "s/\\bKindEnum\\b/${name}KindEnum/g" "$f"
done
perl -pi -e 's/\b([A-Za-z0-9]+)\.KindEnum\b/$1.$1KindEnum/g' Generated/Client/ClientUtils.cs

# --- Emit AOT-safe JSON defaults from the fresh output -------------------------------------
# Explicit converter registrations + a source-generation context for every model.
# Replaces runtime reflection (Assembly.GetTypes + Activator) so the client works
# under trimming and NativeAOT. The diff of this file is the review surface when
# the spec changes: new models appear as new [JsonSerializable] entries.
# Top-level declarations sit at 4-space indent inside the block namespace; this anchor
# excludes nested helper types (e.g. the per-OneOf `KindEnum` classes), which must not
# become [JsonSerializable] roots (name collisions: SYSLIB1031).
MODELS="$(
    grep -rhoE '^    public (partial )?(class|enum) [A-Za-z0-9_]+' Generated/Model \
        | awk '{ print $NF }' \
        | grep -v 'JsonConverter$' \
        | sort -u
)"
ENUMS="$(
    grep -rhoE '^    public enum [A-Za-z0-9_]+' Generated/Model \
        | awk '{ print $NF }' \
        | sort -u
)"
CONVERTERS="$(
    {
        grep -rhoE '^    public (partial )?class [A-Za-z0-9_]+' Generated/Model Generated/Client \
            | awk '{ print $NF }' \
            | grep 'JsonConverter$' \
            | sed 's/^ResourceRefJsonConverter$/ResourceRefDiscriminatedJsonConverter/'
        # Hand-written, in Data/: reads every timestamp as UTC.
        echo UtcDateTimeJsonConverter
    } | sort -u
)"
# Closed generics (de)serialized inside converters (List<X>, Dictionary<...>) are never
# reached from model property graphs — root them explicitly so runtime lookups never miss.
# `object` is rooted for the free-form (metadata/graphExpansion) properties, which resolve
# to JsonElement under source generation exactly as they do under reflection.
GENERICS="$(
    grep -rhoE 'JsonSerializer\.(Deserialize|Serialize)(Async)?<[A-Za-z0-9_ <>,?]+>' Generated/ \
        | sed -E 's/^JsonSerializer\.(Deserialize|Serialize)(Async)?<//; s/>$//' \
        | grep '<' \
        | sort -u
)"

{
    cat <<'HEADER'
// <auto-generated>
// Emitted by generate.sh from the freshly generated models — do not edit by hand.
// Explicit converter registrations plus source-generation metadata for every model:
// no Assembly.GetTypes()/Activator reflection, so the client works under trimming and NativeAOT.

using System.Text.Json.Serialization;
using System.Collections.Generic;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory.Client;

/// <summary>
/// Source-generated serializer context covering every Agent Memory model, with every
/// generated converter pre-registered. <see cref="AgentMemoryJsonContext.Default"/> is what
/// <see cref="SurrealDb.AgentMemory.AgentMemoryClient"/> reads and writes the models with.
/// </summary>
[JsonSourceGenerationOptions(Converters = new[]
{
HEADER
    printf '    typeof(%s),\n' $CONVERTERS
    printf '})]\n'
    printf '[JsonSerializable(typeof(%s))]\n' $MODELS
    # Generated converters deserialize nullable enum fields directly (e.g. Tier?).
    # Root these closed types as well; enum roots alone do not provide their metadata.
    for enum in $ENUMS; do
        printf '[JsonSerializable(typeof(%s?))]\n' "$enum"
    done
    # DateTime is parsed directly by generated converters but is not a model root.
    printf '[JsonSerializable(typeof(DateTime))]\n'
    printf '[JsonSerializable(typeof(DateTime?))]\n'
    # line-wise (not word-split) — generic type args contain spaces
    printf '%s\n' "$GENERICS" | while IFS= read -r g; do
        printf '[JsonSerializable(typeof(%s))]\n' "$g"
    done
    printf '[JsonSerializable(typeof(object))]\n'
    # What a free-form `object` property holds once it has been read, so it can be written back.
    printf '[JsonSerializable(typeof(System.Text.Json.JsonElement))]\n'
    cat <<'FOOTER'
public partial class AgentMemoryJsonContext : JsonSerializerContext
{
}
FOOTER
} > Generated/Client/AgentMemoryJson.g.cs
# ---------------------------------------------------------------------------------------------

# The generator skips the marker on some support files; add it so analyzers/IDE treat every file under Generated/ as generated.
for f in $(find Generated -name '*.cs'); do
    head -1 "$f" | grep -q '<auto-generated>' || perl -pi -e 'print "// <auto-generated>\n" if $. == 1' "$f"
done

# The generator calls JsonSerializer.Deserialize<T>(ref reader, options) and
# JsonSerializer.Serialize(writer, value, options), which trimming and NativeAOT flag because they
# can fall back to reflection. Route them through AgentMemoryJsonSerializer (Data/), which resolves
# the JsonTypeInfo from the options instead, so an application that references this package gets no
# trim or AOT warnings from it. The library builds with the AOT analyzers and warnings as errors, so
# a call this rewrite misses fails the build.
for f in $(grep -rl 'JsonSerializer\.' Generated); do
    perl -pi -e 's/\bJsonSerializer\.(Deserialize<|Serialize\()/global::SurrealDb.AgentMemory.Client.AgentMemoryJsonSerializer.$1/g' "$f"
done

echo "Done. Review the diff, then bump <Version> in SurrealDb.AgentMemory.csproj if the spec version changed."
