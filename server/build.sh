#!/usr/bin/env bash

echo "Cleaning..."

rm -rf ./dist

echo "Building..."

# do not publish a single file as it screws up SimConnect DLLs
dotnet publish ./src/server.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false /p:PublishDir=../dist/win-x64

echo "Copying..."

cp ./src/default-config.json ./dist/win-x64/config.json

echo "Zipping..."

(cd ./dist/win-x64 && zip -r ../server-win-x64.zip .)

unzip -Z1 ./dist/win-x64.zip

echo "Done"