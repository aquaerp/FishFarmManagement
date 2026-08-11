[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactsRoot
)

$ErrorActionPreference = "Stop"
$releaseRoot = Join-Path $ArtifactsRoot "FishFarmManager-bin\Release"
$assemblies = @(Get-ChildItem -LiteralPath $releaseRoot -Recurse -File -Filter "FishFarmManager.dll" -ErrorAction SilentlyContinue)
if ($assemblies.Count -ne 1)
{
    throw "Expected exactly one Release assembly below $releaseRoot; found $($assemblies.Count)."
}
$assembly = $assemblies[0].FullName

$content = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($assembly))
$forbiddenMarkers = @(
    "admin@aquafarm.com",
    "manager@aquafarm.com",
    "accountant@aquafarm.com",
    "production@aquafarm.com",
    "sales@aquafarm.com"
)

$found = @($forbiddenMarkers | Where-Object { $content.Contains($_) })
if ($found.Count -gt 0)
{
    throw "Release assembly contains demo identity markers: $($found -join ', ')"
}

Write-Host "Release package policy passed: no demo identity markers were found."
