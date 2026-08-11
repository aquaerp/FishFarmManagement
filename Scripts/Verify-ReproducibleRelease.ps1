[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version = '1.0.0',
    [string]$OutputRoot = (Join-Path $env:TEMP 'AquaFarm-Reproducible-Release')
)

$ErrorActionPreference = 'Stop'
$packager = Join-Path $PSScriptRoot 'New-ReleasePackage.ps1'
$outputRoot = [IO.Path]::GetFullPath($OutputRoot)
$run1 = Join-Path $outputRoot 'run-1'
$run2 = Join-Path $outputRoot 'run-2'
$archiveName = "AquaFarmPro-$Version-win-x64.zip"

& $packager -Version $Version -OutputRoot $run1 -SkipTests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $packager -Version $Version -OutputRoot $run2 -SkipTests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$firstArchive = Join-Path $run1 $archiveName
$secondArchive = Join-Path $run2 $archiveName
$firstHash = (Get-FileHash -LiteralPath $firstArchive -Algorithm SHA256).Hash.ToLowerInvariant()
$secondHash = (Get-FileHash -LiteralPath $secondArchive -Algorithm SHA256).Hash.ToLowerInvariant()
if ($firstHash -ne $secondHash)
{
    throw "Release is not reproducible. Run 1: $firstHash; Run 2: $secondHash"
}

foreach ($suffix in @('.zip', '.zip.sha256', '.manifest.json'))
{
    $source = Join-Path $run1 ("AquaFarmPro-$Version-win-x64$suffix")
    Copy-Item -LiteralPath $source -Destination $outputRoot -Force
}

Write-Host "Reproducible release verified: $firstHash"
Write-Host "Evidence directory: $outputRoot"
