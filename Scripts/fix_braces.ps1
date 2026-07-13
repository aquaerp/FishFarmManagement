# Script to fix missing closing braces in C# files
# جميع الملفات التي لديها أقواس ناقصة

$files = @(
    "Forms\SupplierPaymentForm.cs",
    "Forms\MainForm.cs",
    "Forms\CostAnalysisReportForm.cs",
    "Forms\TreatmentReportForm.cs",
    "Forms\CostRecordForm.cs",
    "Forms\CustomerPaymentForm.cs",
    "Forms\PondManagementForm.cs",
    "Forms\PondPerformanceReportForm.cs",
    "Forms\EnvironmentalReportForm.cs",
    "Forms\FeedingReportForm.cs",
    "Forms\FishHealthReportForm.cs",
    "Forms\SupplierForm.cs",
    "Services\BackupService.cs"
)

foreach ($file in $files) {
    $fullPath = "E:\Fish Management\FishFarmManager\$file"
    
    if (Test-Path $fullPath) {
        Write-Host "Checking: $file" -ForegroundColor Yellow
        
        $content = Get-Content $fullPath -Raw
        
        # Count opening and closing braces
        $openBraces = ($content.ToCharArray() | Where-Object { $_ -eq '{' }).Count
        $closeBraces = ($content.ToCharArray() | Where-Object { $_ -eq '}' }).Count
        
        Write-Host "  Opening braces: $openBraces" -ForegroundColor Cyan
        Write-Host "  Closing braces: $closeBraces" -ForegroundColor Cyan
        
        if ($openBraces -gt $closeBraces) {
            $diff = $openBraces - $closeBraces
            Write-Host "  Missing $diff closing brace(s)" -ForegroundColor Red
            
            # Add missing closing braces at the end
            $content = $content.TrimEnd()
            for ($i = 0; $i -lt $diff; $i++) {
                $content += "`n}"
            }
            
            Set-Content -Path $fullPath -Value $content -NoNewline
            Write-Host "  FIXED!" -ForegroundColor Green
        }
        elseif ($closeBraces -gt $openBraces) {
            Write-Host "  Extra closing braces!" -ForegroundColor Magenta
        }
        else {
            Write-Host "  OK" -ForegroundColor Green
        }
    }
    else {
        Write-Host "Not found: $file" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "Done!" -ForegroundColor Green
