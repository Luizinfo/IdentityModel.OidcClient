$ErrorActionPreference = "Stop";

if ([string]::IsNullOrWhiteSpace($env:OIDCCLIENT_NUGET_CONFIG)) {
    dotnet restore .\build\build.csproj --nologo
}
else {
    dotnet restore .\build\build.csproj --configfile $env:OIDCCLIENT_NUGET_CONFIG --nologo
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet run --project build --no-restore -- $args
exit $LASTEXITCODE
