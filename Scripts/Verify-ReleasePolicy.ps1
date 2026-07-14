[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactsRoot
)

$ErrorActionPreference = "Stop"
$assembly = Join-Path $ArtifactsRoot "FishFarmManager-bin\Release\net8.0-windows\FishFarmManager.dll"
if (!(Test-Path -LiteralPath $assembly))
{
    throw "Release assembly not found: $assembly"
}

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
