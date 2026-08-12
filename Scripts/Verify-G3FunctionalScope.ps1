[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$targets = @('Forms', 'Services')
$excluded = @((Join-Path $root 'Services\BackupService.cs'))
$patterns = @('TODO', 'FIXME', 'NotImplementedException')
$findings = foreach ($directory in $targets) {
    Get-ChildItem (Join-Path $root $directory) -Filter '*.cs' -Recurse | Where-Object {
        $_.FullName -notin $excluded
    } | Select-String -SimpleMatch -CaseSensitive -Pattern $patterns
}

if ($findings) {
    $findings | ForEach-Object { Write-Error "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    throw 'G3 functional-scope verification failed.'
}

Write-Host 'G3 functional-scope verification passed.'
