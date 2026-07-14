[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$solution = Join-Path $PSScriptRoot "..\FishFarmManager.sln"
$output = dotnet list $solution package --vulnerable --include-transitive 2>&1 | Out-String
$exitCode = $LASTEXITCODE
$output | Write-Host

if ($exitCode -ne 0)
{
    exit $exitCode
}

if ($output -match "has the following vulnerable packages")
{
    Write-Error "NuGet vulnerability audit found one or more vulnerable packages."
    exit 1
}

Write-Host "NuGet vulnerability audit passed: no vulnerable packages were reported."
