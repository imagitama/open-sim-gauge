#!/usr/bin/env bash

echo "Cleaning..."

rm -rf ./dist

echo "Building win-x64..."

dotnet publish ./src/client.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishDir=../dist/win-x64

echo "Copying..."

cp ./src/default-config.json ./dist/win-x64/config.json
cp -R ./src/fonts ./dist/win-x64
cp -R ../gauges ./dist/win-x64

echo "Zipping..."

(cd ./dist/win-x64 && zip -r ../client-win-x64.zip .)
unzip -Z1 ./dist/client-win-x64.zip

echo "Building osx-arm64..."

dotnet publish ./src/client.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true /p:PublishDir=../dist/osx-arm64

echo "Copying..."

cp ./src/default-config.json ./dist/osx-arm64/config.json
cp -R ./src/fonts ./dist/osx-arm64
cp -R ../gauges ./dist/osx-arm64

echo "Zipping..."

(cd ./dist/osx-arm64 && zip -r ../client-osx-arm64.zip .)
unzip -Z1 ./dist/client-osx-arm64.zip

echo "Building linux-x64..."

dotnet publish ./src/client.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:PublishDir=../dist/linux-x64

echo "Copying..."

cp ./src/default-config.json ./dist/linux-x64/config.json
cp -R ./src/fonts ./dist/linux-x64
cp -R ../gauges ./dist/linux-x64

echo "Zipping..."

(cd ./dist/linux-x64 && zip -r ../client-linux-x64.zip .)
unzip -Z1 ./dist/client-linux-x64.zip

echo "Done"