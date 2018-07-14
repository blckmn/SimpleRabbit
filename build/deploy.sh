#!/usr/bin/env bash

set -e

basepath="$PWD"
artifacts="$basepath/artifacts"
nuget_server="https://www.nuget.org/api/v2/package"
revision=$(printf "rel-%d" ${TRAVIS_JOB_ID:=1}) 

if [ -d $artifacts ]; then  
  rm -R $artifacts
fi

echo "Base path: $basepath"
echo "Artifacts: $artifacts"
echo "Revision:  $revision"

dotnet pack -c Release -o $artifacts --version-suffix=$revision 
dotnet nuget push "$artifacts/*.nupkg" -s $nuget_server -k $nuget_access_key
