#!/usr/bin/env bash

set -e

# version
major="6"
minor="0"

basepath="${PWD}"
artifacts="${basepath}/artifacts"
nuget_server="https://www.nuget.org/api/v2/package"
branch=${GITHUB_REF:="unknown"}
buildnumber=${GITHUB_RUN_NUMBER:=1}
version="${major}.${minor}.${buildnumber}"

if [ "${1}" == "deploy" ]; then
version=${GITHUB_REF_NAME}
fi

export VERSION=${version}

if [ -d ${artifacts} ]; then  
  rm -R ${artifacts}
fi

echo "Base path: ${basepath}"
echo "Artifacts: ${artifacts}"
echo "Branch:    ${branch}"
echo "Build #:   ${buildnumber}"
echo "Version:   ${version}"
echo "Secret:    ${NUGET_AUTH_TOKEN:="not supplied"}"

dotnet nuget locals all --clear

dotnet restore

dotnet build -c Release

if [ "${1}" == "deploy" ]; then
  dotnet pack -c Release -o ${artifacts} /p:PackageVersion=${version} --no-dependencies

  dotnet nuget push "${artifacts}/SimpleRabbit*.nupkg" -s ${nuget_server} -k ${NUGET_AUTH_TOKEN}
fi
