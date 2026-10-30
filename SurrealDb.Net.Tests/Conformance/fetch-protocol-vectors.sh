#!/usr/bin/env bash
# Fetches the CBOR test vectors from github.com/surrealdb/surrealdb-protocol at a release tag,
# into Conformance/surrealdb-protocol (ignored by git). Run it before building the tests:
#
#   ./fetch-protocol-vectors.sh            # the pinned tag
#   ./fetch-protocol-vectors.sh v0.15.0    # another tag
#
# The vectors are fetched rather than committed while the protocol repository is under BUSL-1.1.
# Once a release ships under Apache-2.0, vendor them from that tag as spec/README.md recommends.
set -euo pipefail

tag="${1:-v0.14.0}"
here="$(cd "$(dirname "$0")" && pwd)"
dest="$here/surrealdb-protocol"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

git -c advice.detachedHead=false clone --quiet --depth 1 --branch "$tag" https://github.com/surrealdb/surrealdb-protocol.git "$tmp/repo"

rm -rf "$dest"
mkdir -p "$dest/spec/vectors"
cp "$tmp/repo/spec/vectors/cbor.yaml" "$tmp/repo/spec/vectors/cbor2.yaml" "$dest/spec/vectors/"
echo "$tag" > "$dest/VERSION"

echo "Fetched surrealdb-protocol $tag into $dest"
