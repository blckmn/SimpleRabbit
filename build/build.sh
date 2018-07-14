#!/usr/bin/env bash

set -e

basepath="$PWD"
artifacts="$basepath/artifacts"
revision=${TRAVIS_BRANCH:="alpha"}
major="1"
minor="0"
buildnumber=${TRAVIS_BUILD_NUMBER:=1}
version="${major}.${minor}.${buildnumber}"

if [ "$revision" != "release" ]; then
  version="${version}-${revision}"
fi
export VERSION=${version}

echo "Base path: $basepath"
echo "Artifacts: $artifacts"
echo "Revision:  $revision"
echo "Build #:   $buildnumber"
echo "Version:   $version"

dotnet restore

dotnet build -c Release
