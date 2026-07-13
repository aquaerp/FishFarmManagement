# ======================================================
# Quick Rebuild Script - سكريبت إعادة البناء السريع
# FishFarmManager Project
# ======================================================
# 
# الهدف: حل مشكلة قفل الملفات (CS1504) وإعادة بناء المشروع
# الاستخدام: .\quick-rebuild.ps1
# 
# ======================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     🔧 سكريبت إعادة البناء السريع                 ║" -ForegroundColor Cyan
Write-Host "║     Quick Rebuild Script                          ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# الخطوة 1: إيقاف العمليات المعلقة
Write-Host "🔄 الخطوة 1: إيقاف العمليات المعلقة..." -ForegroundColor Yellow
try {
    $processes = Get-Process MSBuild,VBCSCompiler -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force
        Write-Host "   ✅ تم إيقاف $($processes.Count) عملية" -ForegroundColor Green
    } else {
        Write-Host "   ℹ️  لا توجد عمليات معلقة" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ⚠️  تحذير: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# الخطوة 2: تنظيف المشروع
Write-Host "🧹 الخطوة 2: تنظيف المشروع..." -ForegroundColor Yellow
try {
    $cleanOutput = dotnet clean 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ تم التنظيف بنجاح" -ForegroundColor Green
    } else {
        Write-Host "   ❌ فشل التنظيف" -ForegroundColor Red
        Write-Host "   $cleanOutput" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ❌ خطأ: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# الخطوة 3: إعادة البناء
Write-Host "🔨 الخطوة 3: إعادة بناء المشروع..." -ForegroundColor Yellow
try {
    $buildOutput = dotnet build --verbosity minimal 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ تم البناء بنجاح!" -ForegroundColor Green
        
        # عد الأخطاء والتحذيرات
        $warnings = ($buildOutput | Select-String "warning").Count
        $errors = ($buildOutput | Select-String "error" | Where-Object { $_ -notmatch "0 error" }).Count
        
        Write-Host ""
        Write-Host "📊 النتائج:" -ForegroundColor Cyan
        Write-Host "   ❌ الأخطاء: $errors" -ForegroundColor $(if ($errors -eq 0) { "Green" } else { "Red" })
        Write-Host "   ⚠️  التحذيرات: $warnings" -ForegroundColor $(if ($warnings -eq 0) { "Green" } else { "Yellow" })
        
    } else {
        Write-Host "   ❌ فشل البناء!" -ForegroundColor Red
        Write-Host ""
        Write-Host "📋 الأخطاء:" -ForegroundColor Red
        $buildOutput | Select-String "error" | ForEach-Object {
            Write-Host "   $_" -ForegroundColor Red
        }
        exit 1
    }
} catch {
    Write-Host "   ❌ خطأ: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║     ✅ اكتمل بنجاح! Build Successful!            ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "💡 يمكنك الآن تشغيل التطبيق بـ: dotnet run" -ForegroundColor Cyan
Write-Host ""

