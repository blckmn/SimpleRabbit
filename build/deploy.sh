#!/usr/bin/env bash

set -e

basepath="$PWD"
artifacts="$basepath/artifacts"
nuget_server="https://www.nuget.org/api/v2/package"
revision=${TRAVIS_BRANCH:="alpha"}
major="1"
minor="0"
buildnumber=${TRAVIS_BUILD_NUMBER:=1}
version="${major}.${minor}.${buildnumber}"

if [ "$revision" != "release" ]; then
  version="${version}-${revision}"
fi
export VERSION=${version}

if [ -d $artifacts ]; then  
  rm -R $artifacts
fi

echo "Base path: $basepath"
echo "Artifacts: $artifacts"
echo "Revision:  $revision"
echo "Build #:   $buildnumber"
echo "Version:   $version"

if [ "$revision" == "release" ]; then
  export BUILD=""
  dotnet pack -c Release -o $artifacts 
else
  export BUILD=${revision}
  dotnet pack -c Release -o $artifacts --version-suffix=$revision 
fi

dotnet nuget push "$artifacts/*.nupkg" -s $nuget_server -k $nuget_access_key
