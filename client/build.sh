#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(realpath "$SCRIPT_DIR/..")"

version=$(<"$SCRIPT_DIR/../VERSION.txt")
default_platforms=("win-x64" "osx-arm64" "linux-x64")
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

build_project() {
    local project_path=$1
    local project_name=$2
    local startup_object=$3
    local type=$4

    for platform in "${platforms[@]}"; do
        echo "Building project '$project_name' path '$project_path' for '$platform' with startup '$startup_object'..."

        dotnet publish "$project_path" -c Release -r "$platform" --self-contained true \
            /p:PublishSingleFile=true \
            /p:PublishTrimmed=false \
            /p:PublishDir=../../dist/"$platform"
        
        echo "Renaming..."

        ext=""
        if [[ "$platform" == win* ]]; then
            ext=".exe"
        fi

        mv "$SCRIPT_DIR/dist/$platform/$project_name$ext" "$SCRIPT_DIR/dist/$platform/$type$ext"

        echo "Copying resources..."
        
        cp "$SCRIPT_DIR/src/default-config.json" "$SCRIPT_DIR/dist/$platform/config.json"
        cp -R "$SCRIPT_DIR/src/fonts" "$SCRIPT_DIR/dist/$platform"
        cp -R "$SCRIPT_DIR/../gauges" "$SCRIPT_DIR/dist/$platform"

        cp "$SCRIPT_DIR/README.md" "$SCRIPT_DIR/dist/$platform"

        echo "Zipping..."
        zip_name="OpenSimGauge-${version}-${platform}.zip"
        (
            cd "$SCRIPT_DIR/dist/$platform" || exit
            zip -r "../$zip_name" . -x "*.DS_Store" "__MACOSX/*"
        )
    done
}

generate_readme

build_project "$SCRIPT_DIR/src/client/OpenSimGaugeClient.csproj" "OpenSimGaugeClient" "OpenGaugeClient.Client.Program" client

build_project "$SCRIPT_DIR/src/editor/OpenSimGaugeEditor.csproj" "OpenSimGaugeEditor" "OpenGaugeEditor.Editor.Program" editor

echo "Done!"
