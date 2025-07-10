#!/bin/bash
VERSION=$1
RELEASE_NOTES=$2

dotnet restore
dotnet build -c Release --no-restore
dotnet pack -c Release --no-build -p:Version="$VERSION" -p:PackageReleaseNotes="$RELEASE_NOTES" -o ./artifacts