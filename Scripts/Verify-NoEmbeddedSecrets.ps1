[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$violations = @()

$seedFile = Join-Path $root "Data\DataSeeder.cs"
$seedText = [IO.File]::ReadAllText($seedFile)
if ($seedText -match 'PasswordHash\s*=\s*HashPassword\s*\(\s*"')
{
    $violations += "DataSeeder contains an embedded password literal."
}

foreach ($configName in @("appsettings.json", "appsettings.Production.template.json"))
{
    $configPath = Join-Path $root $configName
    $configText = [IO.File]::ReadAllText($configPath)
    if ($configText -match '"(?:Password|ApiKey|Secret|Token)"\s*:\s*"[^"\s]+"')
    {
        $violations += "$configName contains a non-empty secret-like value."
    }
}

if ($violations.Count -gt 0)
{
    $violations | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Embedded-secret policy check passed."
