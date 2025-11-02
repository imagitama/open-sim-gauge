#!/usr/bin/env bash
set -e

version=$(<../VERSION.txt)
platform="win-x64"

echo "Build $platform $version"

echo "Cleaning..."
rm -rf ./dist

echo "Building..."
dotnet publish ./src/server.csproj -c Release -r "$platform" --self-contained false /p:PublishSingleFile=false /p:PublishDir=../dist/"$platform"

echo "Copying..."
cp ./src/default-config.json ./dist/"$platform"/config.json

echo "Zipping..."
zip_name="server-${version}-${platform}.zip"
(cd ./dist/"$platform" && zip -r "../$zip_name" .)

echo "Created ./dist/$zip_name"
echo "Done"
