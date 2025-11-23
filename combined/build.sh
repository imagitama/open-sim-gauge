#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(realpath "$SCRIPT_DIR/..")"

version=$(<"$SCRIPT_DIR/../VERSION.txt")
default_platforms=("win-x64" "win-arm64" "osx-arm64" "linux-x64" "linux-arm64")
platforms=("$@")

# If no argument given, use all platforms
if [ ${#platforms[@]} -eq 0 ]; then
    platforms=("${default_platforms[@]}")
fi

echo "Cleaning..."
rm -rf "$SCRIPT_DIR/dist"

generate_readme()
{
    echo "Generating README..."

    python3 "$ROOT_DIR"/tools/generate-md-table-from-cs/main.py "$SCRIPT_DIR/src/shared/ConfigManager.cs"

    python3 "$ROOT_DIR"/tools/insert-into-readme/main.py --file "$SCRIPT_DIR"/README.md --section config --input "$ROOT_DIR"/tools/generate-md-table-from-cs/ConfigManager.md
}

build_data_sources()
{
    local platform=$1

    echo "Building data sources..."

    DATA_SRC_ROOT="$SCRIPT_DIR/../server/src/data-sources"
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
            -v:detailed \
            /p:PublishSingleFile=false \
            /p:PublishDir="$OUT_DIR"

        DEST="$DATA_OUT_ROOT"
        mkdir -p "$DEST"

        cp "$OUT_DIR"/*.dll "$DEST"/
    done
}

build_project() {
    local project_path=$1
    local project_name=$2
    local startup_object=$3
    local type=$4

    for platform in "${platforms[@]}"; do
        echo "Building project '$project_name' path '$project_path' for '$platform' with startup '$startup_object'..."

        dotnet publish "$project_path" \
            -c Release \
            -r "$platform" \
            --self-contained true \
            -v:detailed \
            /p:PublishSingleFile=true \
            /p:PublishTrimmed=false \
            /p:PublishDir=./dist/"$platform"

        build_data_sources $platform

        echo "Copying resources..."

        cp "$SCRIPT_DIR/../README.md" "$SCRIPT_DIR/dist/$platform"
        cp "$SCRIPT_DIR/../client/README.md" "$SCRIPT_DIR/dist/$platform/README-client.md"
        cp "$SCRIPT_DIR/../server/README.md" "$SCRIPT_DIR/dist/$platform/README-server.md"

        echo "Zipping..."
        zip_name="OpenSimGauge-${version}-${platform}.zip"
        (
            cd "$SCRIPT_DIR/dist/$platform" || exit
            zip -r "../$zip_name" . -x "*.DS_Store" "__MACOSX/*"
        )
    done
}

build_project "$SCRIPT_DIR/combined.csproj" "Combined" "Program" combined

# generate_readme

echo "Done!"
