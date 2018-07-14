#!/usr/bin/env bash

set -e

buildnumber=${TRAVIS_BUILD_NUMBER:=1}
export BUILD_BUILDNUMBER=${buildnumber}

dotnet restore

dotnet build -c Release
