#!/usr/bin/env bash
set -e

version=$(<../VERSION.txt)
platforms=("win-x64" "osx-arm64" "linux-x64")

echo "Cleaning..."
rm -rf ./dist

for platform in "${platforms[@]}"; do
    echo "Building $platform..."

    dotnet publish ./src/client.csproj -c Release -r "$platform" --self-contained true /p:PublishSingleFile=true /p:PublishDir=../dist/"$platform"

    echo "Copying..."
    cp ./src/default-config.json ./dist/"$platform"/config.json
    cp -R ./src/fonts ./dist/"$platform"
    cp -R ../gauges ./dist/"$platform"

    echo "Zipping..."
    zip_name="client-${version}-${platform}.zip"
    (
    cd ./dist/"$platform" || exit
    zip -r "../$zip_name" . -x "*.DS_Store" "__MACOSX/*"
    )
    unzip -Z1 ./dist/"$zip_name"
done

echo "Done"
