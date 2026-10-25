#!/usr/bin/env bash
# Vendors the CBOR test vectors from github.com/surrealdb/surrealdb-protocol at a release tag.
#
#   ./update-protocol-vectors.sh v0.14.0
#
# The spec asks SDKs to copy the vectors rather than fetch them at test time, and to bump them
# when the protocol repository releases. Commit the result, then fix or list any new failures.
set -euo pipefail

tag="${1:?usage: $0 <tag>}"
here="$(cd "$(dirname "$0")" && pwd)"
dest="$here/surrealdb-protocol"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

git clone --quiet --depth 1 --branch "$tag" https://github.com/surrealdb/surrealdb-protocol.git "$tmp/repo"

mkdir -p "$dest/spec/vectors"
cp "$tmp/repo/spec/vectors/cbor.yaml" "$tmp/repo/spec/vectors/cbor2.yaml" "$dest/spec/vectors/"
cp "$tmp/repo/LICENSE" "$dest/LICENSE"
echo "$tag" > "$dest/VERSION"

echo "Vendored surrealdb-protocol $tag into $dest"
