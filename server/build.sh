#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

version=$(<"$SCRIPT_DIR/../VERSION.txt")
default_platform="win-x64" # "osx-arm64" "linux-x64"
platform=("$@")

if [ -z "$platform" ]; then
    platform="$default_platform"
fi

echo "Build $platform $version"

echo "Cleaning..."
rm -rf ./dist

echo "Building server..."
dotnet publish "$SCRIPT_DIR/src/server/server.csproj" -c Release -r "$platform" --self-contained false /p:PublishSingleFile=false /p:PublishDir="$SCRIPT_DIR"/dist/"$platform"

echo "Building data sources..."

DATA_SRC_ROOT="$SCRIPT_DIR/src/data-sources"
DATA_OUT_ROOT="$SCRIPT_DIR/dist/$platform/data-sources"

mkdir -p "$DATA_OUT_ROOT"

for projdir in "$DATA_SRC_ROOT"/*/ ; do
    [ -d "$projdir" ] || continue

    name="$(basename "$projdir")"
    csproj=$(find "$projdir" -maxdepth 1 -name '*.csproj' | head -n 1)

    if [ -z "$csproj" ]; then
        continue
    fi

    echo "  Building data source: $name"

    OUT_DIR="$projdir/bin/publish"
    rm -rf "$OUT_DIR"

    dotnet publish "$csproj" \
        -c Release -r "$platform" \
        --self-contained false \
        /p:PublishSingleFile=false \
        /p:PublishDir="$OUT_DIR"

    DEST="$DATA_OUT_ROOT"
    mkdir -p "$DEST"

    cp "$OUT_DIR/$name.dll" "$DEST"/

    if [ -d "$projdir/libs" ]; then
        cp -r "$projdir/libs" "$DEST/libs"
    fi
done

echo "Copying..."
cp ./src/default-config.json "$SCRIPT_DIR/dist/$platform/config.json"

echo "Zipping..."
zip_name="server-${version}-${platform}.zip"
(cd "$SCRIPT_DIR/dist/$platform" && zip -r "$SCRIPT_DIR/dist/$zip_name" .)

# (cd "$SCRIPT_DIR/dist" && zip -r "$SCRIPT_DIR/dist/$zip_name" "data-sources")

echo "Created $SCRIPT_DIR/dist/$zip_name"
echo "Done"
