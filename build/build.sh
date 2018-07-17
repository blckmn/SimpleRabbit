#!/usr/bin/env bash

set -e

source "${PWD}/build/common.sh"

dotnet restore

dotnet build -c Release
