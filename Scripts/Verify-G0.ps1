[CmdletBinding()]
param(
    [string]$ArtifactsRoot = (Join-Path $env:TEMP ("AquaFarm-G0-" + [Guid]::NewGuid().ToString("N")))
)

$ErrorActionPreference = "Stop"
$solution = Join-Path $PSScriptRoot "..\FishFarmManager.sln"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_CLI_USE_MSBUILD_SERVER = "0"
$env:MSBUILDDISABLENODEREUSE = "1"

dotnet restore $solution --force --disable-parallel `
    -p:RestoreNoHttpCache=true `
    -p:RestoreFallbackFolders='' `
    -p:Phase0ArtifactsRoot=$ArtifactsRoot `
    -m:1 `
    -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solution -c Release --no-restore `
    -p:Phase0ArtifactsRoot=$ArtifactsRoot `
    -m:1 `
    -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test $solution -c Release --no-build `
    -p:Phase0ArtifactsRoot=$ArtifactsRoot `
    -m:1 `
    -nr:false
exit $LASTEXITCODE
