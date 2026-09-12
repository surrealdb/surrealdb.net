#!/usr/bin/env sh
# Regenerates Generated/ from spec/openapi.json using openapi-generator-cli (see /openapitools.json).
# The generator's own DI extensions (Extensions/), test project and csproj are intentionally
# not copied: dependency injection is provided by AgentMemoryServiceCollectionExtensions.cs.
set -eu

OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT

openapi-generator-cli generate \
    -g csharp \
    -i spec/openapi.json \
    -o "$OUT" \
    --additional-properties 'library=generichost,packageName=SurrealDb.AgentMemory,packageVersion=0.2.1,targetFramework=netstandard2.1;net8.0;net9.0;net10.0,nullableReferenceTypes=true,netCoreProjectFile=true,optionalProjectFile=false,optionalAssemblyInfo=false'

rm -rf Generated
mkdir -p Generated

for dir in Api Client Logging Model; do
    cp -r "$OUT/src/SurrealDb.AgentMemory/$dir" Generated/
done

# Only used by the generator's own (uncopied) DI extensions; not nullable-clean.
rm Generated/Client/HostConfiguration.cs

echo "Done. Review the diff, then bump <Version> in SurrealDb.AgentMemory.csproj if the spec version changed."
