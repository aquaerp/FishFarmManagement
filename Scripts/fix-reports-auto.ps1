# ======================================================
# سكريبت إصلاح التقارير التلقائي
# Automatic Reports Fix Script
# ======================================================
#
# الهدف: إضافة AsNoTracking و Dispose لجميع ملفات التقارير
# الاستخدام: .\fix-reports-auto.ps1
#
# ======================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     🔧 سكريبت إصلاح التقارير التلقائي            ║" -ForegroundColor Cyan
Write-Host "║     Automatic Reports Fix Script                  ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# قائمة ملفات التقارير
$reportFiles = @(
    "Forms\HRReportsForm.cs",
    "Forms\CostAnalysisReportForm.cs",
    "Forms\ProductionReportForm.cs",
    "Forms\FeedingReportForm.cs",
    "Forms\MortalityReportForm.cs",
    "Forms\WaterQualityReportForm.cs",
    "Forms\TreatmentReportForm.cs",
    "Forms\InventoryReportForm.cs",
    "Forms\QualityHealthReportsForm.cs",
    "Forms\PerformanceReportForm.cs",
    "Forms\FishHealthReportForm.cs",
    "Forms\EnvironmentalReportForm.cs",
    "Forms\CostReportForm.cs",
    "Forms\CertificationReportForm.cs",
    "Forms\PondPerformanceReportForm.cs"
)

$totalFiles = $reportFiles.Count
$processedFiles = 0
$modifiedFiles = 0
$errors = 0

Write-Host "🔍 سيتم معالجة $totalFiles ملف..." -ForegroundColor Yellow
Write-Host ""

foreach ($file in $reportFiles) {
    $processedFiles++
    Write-Host "[$processedFiles/$totalFiles] معالجة: $file" -ForegroundColor Cyan
    
    if (-not (Test-Path $file)) {
        Write-Host "   ⚠️  الملف غير موجود - تخطي" -ForegroundColor Yellow
        continue
    }
    
    try {
        $content = Get-Content $file -Raw -Encoding UTF8
        $originalContent = $content
        $modified = $false
        
        # =====================================================
        # التعديل 1: إضافة using System.Threading.Tasks
        # =====================================================
        if ($content -notmatch "using System\.Threading\.Tasks;") {
            $content = $content -replace "(using System;)", "`$1`r`nusing System.Threading.Tasks;"
            Write-Host "   ✅ تم إضافة using System.Threading.Tasks" -ForegroundColor Green
            $modified = $true
        }
        
        # =====================================================
        # التعديل 2: إضافة AsNoTracking للاستعلامات
        # =====================================================
        $asNoTrackingCount = 0
        
        # النمط 1: _context.TableName
        $pattern1 = "(_context\.(SalesOrders|Customers|Employees|InventoryItems|ProductionCycles|Suppliers|Ponds|WaterQualityRecords|FeedingRecords|MortalityRecords|TreatmentRecords|QualityTests|CostRecords|Attendances|Salaries))\s*\r?\n\s*\.(?!AsNoTracking)"
        
        if ($content -match $pattern1) {
            $content = $content -replace $pattern1, "`$1`r`n                .AsNoTracking()`r`n                ."
            $asNoTrackingMatches = ([regex]::Matches($originalContent, $pattern1)).Count
            $asNoTrackingCount += $asNoTrackingMatches
        }
        
        if ($asNoTrackingCount -gt 0) {
            Write-Host "   ✅ تم إضافة AsNoTracking في $asNoTrackingCount موضع" -ForegroundColor Green
            $modified = $true
        }
        
        # =====================================================
        # التعديل 3: إضافة Dispose override
        # =====================================================
        if ($content -notmatch "protected override void Dispose\(bool disposing\)") {
            # إيجاد نهاية الـ Class
            $disposeCode = @"

        /// <summary>
        /// ✅ Dispose override لتحرير الموارد
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _context?.Dispose();
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning("خطأ أثناء Dispose للـ Context: {Error}", ex.Message);
                }
            }
            base.Dispose(disposing);
        }
    }
}
"@
            
            # استبدال آخر } }
            $lastIndex = $content.LastIndexOf("    }")
            if ($lastIndex -gt 0) {
                $content = $content.Substring(0, $lastIndex) + $disposeCode
                Write-Host "   ✅ تم إضافة Dispose override" -ForegroundColor Green
                $modified = $true
            }
        }
        
        # =====================================================
        # حفظ التعديلات
        # =====================================================
        if ($modified) {
            Set-Content -Path $file -Value $content -Encoding UTF8
            $modifiedFiles++
            Write-Host "   💾 تم حفظ التعديلات" -ForegroundColor Green
        } else {
            Write-Host "   ℹ️  لا توجد تعديلات مطلوبة" -ForegroundColor Gray
        }
        
    } catch {
        Write-Host "   ❌ خطأ: $($_.Exception.Message)" -ForegroundColor Red
        $errors++
    }
    
    Write-Host ""
}

# =====================================================
# الملخص النهائي
# =====================================================
Write-Host "╔════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║     ✅ اكتمل المعالجة!                            ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📊 النتائج:" -ForegroundColor Cyan
Write-Host "   ✅ الملفات المعالجة: $processedFiles" -ForegroundColor Green
Write-Host "   💾 الملفات المعدلة: $modifiedFiles" -ForegroundColor Green
Write-Host "   ❌ الأخطاء: $errors" -ForegroundColor $(if ($errors -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($modifiedFiles -gt 0) {
    Write-Host "⚠️  ملاحظة مهمة:" -ForegroundColor Yellow
    Write-Host "   - تم إضافة AsNoTracking و Dispose تلقائياً" -ForegroundColor Yellow
    Write-Host "   - يجب مراجعة الملفات يدوياً للتأكد" -ForegroundColor Yellow
    Write-Host "   - يجب إضافة Async/Await يدوياً (معقد للغاية)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🔨 الخطوات التالية:" -ForegroundColor Cyan
    Write-Host "   1. dotnet build --verbosity minimal" -ForegroundColor White
    Write-Host "   2. راجع الأخطاء إن وجدت" -ForegroundColor White
    Write-Host "   3. أضف Async/Await للمعالجات يدوياً" -ForegroundColor White
}

Write-Host ""

