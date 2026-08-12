[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$violations = @()
$forms = Get-ChildItem (Join-Path $root 'Forms') -Filter '*.cs' -Recurse

foreach ($match in ($forms | Select-String -Pattern 'InnerException'))
{
    $violations += "$($match.Path):$($match.LineNumber): UI code must not expose inner exceptions."
}

foreach ($match in ($forms | Select-String -Pattern 'PasswordHash|PrivateKey|ConnectionString'))
{
    if ($match.Line -match 'MessageBox\.Show|ShowError|ShowInfo|ShowSuccess')
    {
        $violations += "$($match.Path):$($match.LineNumber): UI message references sensitive data."
    }
}

if ($violations.Count -gt 0)
{
    $violations | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'G5 UI data-exposure check passed.'
