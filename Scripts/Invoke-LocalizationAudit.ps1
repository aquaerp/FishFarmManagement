param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = "",
    [switch]$NoOutputFile
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $Root 'Docs\Reports\localization-audit.json'
}

$sourceFiles = Get-ChildItem -Path $Root -Recurse -Filter '*.cs' -File |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|Migrations)[\\/]'
    }
$formFiles = $sourceFiles | Where-Object { $_.FullName -match '[\\/]Forms[\\/]' }

function Count-FilesMatching([System.IO.FileInfo[]]$Files, [string]$Pattern) {
    @($Files | Where-Object { Select-String -LiteralPath $_.FullName -Pattern $Pattern -Quiet }).Count
}

$textAssignments = 0
$arabicLiterals = 0
foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $textAssignments += ([regex]::Matches($content, '\bText\s*=')).Count
    $arabicLiterals += ([regex]::Matches($content, '"[^"\r\n]*[\u0600-\u06FF][^"\r\n]*"')).Count
}

$result = [ordered]@{
    generatedAt = [DateTimeOffset]::Now.ToString('o')
    cultures = @('ar-SA', 'en-GB')
    sourceFiles = @($sourceFiles).Count
    formFiles = @($formFiles).Count
    formFilesWithArabic = Count-FilesMatching $formFiles '[\u0600-\u06FF]'
    formFilesWithExplicitRtl = Count-FilesMatching $formFiles 'RightToLeft\s*=\s*RightToLeft\.Yes'
    formFilesWithFixedLocation = Count-FilesMatching $formFiles '\bLocation\s*='
    formFilesWithFixedSize = Count-FilesMatching $formFiles '\bSize\s*='
    formFilesWithMessageBox = Count-FilesMatching $formFiles '\bMessageBox\.'
    textAssignments = $textAssignments
    arabicStringLiterals = $arabicLiterals
}

if (-not $NoOutputFile) {
    $directory = Split-Path -Parent $OutputPath
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $result | ConvertTo-Json | Set-Content -LiteralPath $OutputPath -Encoding utf8
}
$result | ConvertTo-Json
