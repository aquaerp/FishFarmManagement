param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Publish
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "FishFarmManager.sln"
$publishDir = Join-Path $root "Setup\publish"
$rootFullPath = [System.IO.Path]::GetFullPath($root)
$publishFullPath = [System.IO.Path]::GetFullPath($publishDir)

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

Write-Step "Checking .NET SDK"
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw ".NET SDK was not found on PATH. Install .NET 8 SDK before building a release."
}

$sdkList = & $dotnet.Source --list-sdks
if (-not ($sdkList -match "^8\.")) {
    throw ".NET 8 SDK is required. Installed SDKs:`n$sdkList"
}

Write-Step "Restoring packages"
& $dotnet.Source restore $solution

Write-Step "Building $Configuration"
& $dotnet.Source build $solution -c $Configuration --no-restore

Write-Step "Running tests"
& $dotnet.Source test $solution -c $Configuration --no-build

if ($Publish) {
    Write-Step "Publishing $Runtime"
    if (Test-Path $publishDir) {
        if (-not $publishFullPath.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove publish directory outside the project root: $publishFullPath"
        }
        Remove-Item $publishDir -Recurse -Force
    }

    & $dotnet.Source publish (Join-Path $root "FishFarmManager.csproj") `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -o $publishDir
}

Write-Step "Release readiness checks completed"
