@echo off
if defined OIDCCLIENT_NUGET_CONFIG (
  dotnet restore .\build\build.csproj --configfile "%OIDCCLIENT_NUGET_CONFIG%" --nologo
) else (
  dotnet restore .\build\build.csproj --nologo
)
if errorlevel 1 exit /b 1

dotnet run --project build --no-restore -- %*
exit /b %ERRORLEVEL%
