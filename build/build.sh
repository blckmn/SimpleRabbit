#!/usr/bin/env bash

set -e

# version
major="1"
minor="0"

basepath="${PWD}"
artifacts="${basepath}/artifacts"
nuget_server="https://www.nuget.org/api/v2/package"
revision=${TRAVIS_BRANCH:="alpha"}
buildnumber=${TRAVIS_BUILD_NUMBER:=1}
version="${major}.${minor}.${buildnumber}"

if [ "${revision}" != "release" ]; then
  version="${version}-${revision}"
fi
export VERSION=${version}

if [ -d ${artifacts} ]; then  
  rm -R ${artifacts}
fi

echo "Base path: ${basepath}"
echo "Artifacts: ${artifacts}"
echo "Revision:  ${revision}"
echo "Build #:   ${buildnumber}"
echo "Version:   ${version}"

if [[ ! -e ${artifacts} ]]; then
    mkdir ${artifacts}
fi

dotnet nuget locals all --clear

dotnet restore

dotnet build -c Release

dotnet pack -c Release -o ${artifacts} /p:PackageVersion=${version} --no-dependencies

if [ "${revision}" == "release" ]; then
  dotnet nuget push "${artifacts}/*.nupkg" -s ${nuget_server} -k ${NUGET_ACCESS_KEY}
fi
