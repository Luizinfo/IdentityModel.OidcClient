#!/usr/bin/env bash
set -euo pipefail

if [[ -n "${OIDCCLIENT_NUGET_CONFIG:-}" ]]; then
  dotnet restore ./build/build.csproj --configfile "$OIDCCLIENT_NUGET_CONFIG" --nologo
else
  dotnet restore ./build/build.csproj --nologo
fi

dotnet run --project build --no-restore -- "$@"
