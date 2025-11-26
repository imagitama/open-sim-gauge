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

    python3 "$ROOT_DIR"/tools/generate-md-table-from-cs/main.py "$SCRIPT_DIR/src/shared/models" "$ROOT_DIR"/tools/generate-md-table-from-cs/models.md

    python3 "$ROOT_DIR"/tools/insert-into-readme/main.py --file "$SCRIPT_DIR"/README.md --section config --input "$ROOT_DIR"/tools/generate-md-table-from-cs/models.md
}

build_project()
{
    local project_path=$1
    local project_name=$2
    local platform=$3

    echo "Building project '$project_name' path '$project_path' for '$platform' version $version..."

    out_dir="$SCRIPT_DIR/dist/$project_name-$platform"

    echo "Into: $out_dir"

    dotnet publish "$project_path" \
        -c Release \
        -r "$platform" \
        --self-contained true \
        -v:detailed \
        /p:PublishSingleFile=true \
        /p:PublishTrimmed=false \
        /p:PublishDir=$out_dir \
        /p:AppVersion="$version"

    echo "Copying resources..."

    cp "$SCRIPT_DIR/README.md" $out_dir

    echo "Built successfully"
}

package()
{
    local platform=$1

    echo "Packaging $platform..."

    combined_output_dir="$SCRIPT_DIR/dist/client-editor-$platform"

    echo "From: $combined_output_dir"

    cp -R "$SCRIPT_DIR/dist/client-$platform/" $combined_output_dir
    cp -R "$SCRIPT_DIR/dist/editor-$platform/" $combined_output_dir

    zip_name="client-editor-${version}-${platform}.zip"
    (
        cd $combined_output_dir || exit
        zip -r "../$zip_name" . -x "*.DS_Store" "__MACOSX/*"
    )

    echo "Result: $zip_name"
}

generate_readme

for platform in "${platforms[@]}"; do
    build_project "$SCRIPT_DIR/src/client/client.csproj" client $platform

    build_project "$SCRIPT_DIR/src/editor/editor.csproj" editor $platform

    package $platform
done;

echo "Done!"
