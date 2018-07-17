#!/usr/bin/env bash

set -e

source "${PWD}/build/common.sh"

if [ "${revision}" == "release" ]; then
  dotnet pack -c Release -o ${artifacts} 
else
  dotnet pack -c Release -o ${artifacts} --version-suffix=${revision} 
fi

dotnet nuget push "${artifacts}/*.nupkg" -s ${nuget_server} -k ${NUGET_ACCESS_KEY}
