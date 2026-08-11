[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version = '1.0.0',
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\artifacts\release'),
    [switch]$SkipTests,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $root 'FishFarmManager.sln'
$project = Join-Path $root 'FishFarmManager.csproj'
$outputRoot = [IO.Path]::GetFullPath($OutputRoot)
$publishDirectory = Join-Path $outputRoot 'publish'
$packageDirectory = Join-Path $outputRoot 'package'
$packageName = "AquaFarmPro-$Version-win-x64"
$archivePath = Join-Path $outputRoot "$packageName.zip"
$manifestPath = Join-Path $outputRoot "$packageName.manifest.json"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:MSBUILDDISABLENODEREUSE = '1'

if (!$AllowDirty)
{
    $status = git -C $root status --porcelain
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read Git status.' }
    if ($status) { throw 'Release packaging requires a clean Git working tree.' }
}

$commit = (git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or !$commit) { throw 'Unable to resolve the release commit.' }

if (Test-Path -LiteralPath $outputRoot)
{
    $resolvedOutput = (Resolve-Path -LiteralPath $outputRoot).Path
    if (!$resolvedOutput.StartsWith($root + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase) -and
        !$resolvedOutput.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase))
    {
        throw "Refusing to clean an output directory outside the workspace or temp directory: $resolvedOutput"
    }
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null

dotnet restore $solution --locked-mode --disable-parallel `
    -p:RestoreFallbackFolders='' -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solution -c Release --no-restore `
    -p:Version=$Version -p:ContinuousIntegrationBuild=true -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (!$SkipTests)
{
    dotnet test $solution -c Release --no-build `
        -p:Version=$Version -p:ContinuousIntegrationBuild=true -m:1 -nr:false
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

dotnet restore $project --locked-mode -r win-x64 --disable-parallel `
    -p:RestoreFallbackFolders='' -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore `
    -p:Version=$Version -p:ContinuousIntegrationBuild=true `
    -p:PublishDir="$publishDirectory\" -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$publishedFiles = @(Get-ChildItem -LiteralPath $publishDirectory -Recurse -File |
    Sort-Object { $_.FullName.Substring($publishDirectory.Length) })
$entries = foreach ($file in $publishedFiles)
{
    $relativePath = $file.FullName.Substring($publishDirectory.Length).TrimStart('\', '/').Replace('\', '/')
    [ordered]@{
        path = $relativePath
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        bytes = $file.Length
    }
}

$metadata = [ordered]@{
    product = 'AquaFarm Pro'
    version = $Version
    runtime = 'win-x64'
    selfContained = $true
    commit = $commit
    sdk = (dotnet --version).Trim()
    files = @($entries)
}
$manifestJson = $metadata | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText($manifestPath, $manifestJson, (New-Object Text.UTF8Encoding($false)))
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $packageDirectory 'release-manifest.json')
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination $packageDirectory -Recurse

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$fixedTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
$archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
try
{
    foreach ($file in (Get-ChildItem -LiteralPath $packageDirectory -Recurse -File |
        Sort-Object { $_.FullName.Substring($packageDirectory.Length) }))
    {
        $entryName = $file.FullName.Substring($packageDirectory.Length).TrimStart('\', '/').Replace('\', '/')
        $entry = $archive.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = $fixedTimestamp
        $source = [IO.File]::OpenRead($file.FullName)
        $destination = $entry.Open()
        try { $source.CopyTo($destination) }
        finally { $destination.Dispose(); $source.Dispose() }
    }
}
finally
{
    $archive.Dispose()
}

$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = "$archivePath.sha256"
"$archiveHash  $([IO.Path]::GetFileName($archivePath))" |
    Set-Content -LiteralPath $checksumPath -Encoding ascii -NoNewline

Write-Host "Release package: $archivePath"
Write-Host "Manifest: $manifestPath"
Write-Host "SHA-256: $archiveHash"
