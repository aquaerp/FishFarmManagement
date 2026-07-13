# ========================================
# سكريبت إصلاح الهوية البصرية - AquaFarm Pro
# ========================================
# يقوم باستبدال تلقائي للألوان والخطوط لتتوافق مع دليل الهوية البصرية

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "   إصلاح الهوية البصرية - AquaFarm Pro" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# عداد التغييرات
$totalChanges = 0
$filesModified = 0

# الحصول على جميع ملفات C# في مجلد Forms
$files = Get-ChildItem -Path "Forms\" -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue

if ($files.Count -eq 0) {
    Write-Host "❌ لم يتم العثور على ملفات في مجلد Forms" -ForegroundColor Red
    exit 1
}

Write-Host "📁 تم العثور على $($files.Count) ملف" -ForegroundColor Yellow
Write-Host ""

foreach ($file in $files) {
    Write-Host "⚙️  معالجة: $($file.Name)" -ForegroundColor Gray
    
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        $fileChanges = 0
        
        # ========================================
        # استبدال الألوان
        # ========================================
        
        # الأزرق السماوي المتنوع
        if ($content -match 'Color\.FromArgb\(0, 122, 204\)') {
            $content = $content -replace 'Color\.FromArgb\(0, 122, 204\)', 'ThemeManager.SecondarySkyBlue'
            $fileChanges++
        }
        
        if ($content -match 'Color\.FromArgb\(52, 152, 219\)') {
            $content = $content -replace 'Color\.FromArgb\(52, 152, 219\)', 'ThemeManager.SecondarySkyBlue'
            $fileChanges++
        }
        
        if ($content -match 'Color\.FromArgb\(0, 153, 204\)') {
            $content = $content -replace 'Color\.FromArgb\(0, 153, 204\)', 'ThemeManager.SecondarySkyBlue'
            $fileChanges++
        }
        
        # الأخضر المتنوع
        if ($content -match 'Color\.FromArgb\(46, 204, 113\)') {
            $content = $content -replace 'Color\.FromArgb\(46, 204, 113\)', 'ThemeManager.SuccessGreen'
            $fileChanges++
        }
        
        # الأحمر المتنوع (للأخطاء)
        if ($content -match 'Color\.FromArgb\(231, 76, 60\)') {
            $content = $content -replace 'Color\.FromArgb\(231, 76, 60\)', 'ThemeManager.ErrorRed'
            $fileChanges++
        }
        
        if ($content -match 'Color\.FromArgb\(214, 39, 40\)') {
            $content = $content -replace 'Color\.FromArgb\(214, 39, 40\)', 'ThemeManager.ErrorRed'
            $fileChanges++
        }
        
        # البنفسجي -> أزرق (غير متوافق)
        if ($content -match 'Color\.FromArgb\(155, 89, 182\)') {
            $content = $content -replace 'Color\.FromArgb\(155, 89, 182\)', 'ThemeManager.SecondarySkyBlue'
            $fileChanges++
        }
        
        # البرتقالي -> تحذير
        if ($content -match 'Color\.FromArgb\(255, 127, 0\)') {
            $content = $content -replace 'Color\.FromArgb\(255, 127, 0\)', 'ThemeManager.WarningAmber'
            $fileChanges++
        }
        
        # الأزرق العميق
        if ($content -match 'Color\.FromArgb\(0, 51, 102\)') {
            $content = $content -replace 'Color\.FromArgb\(0, 51, 102\)', 'ThemeManager.PrimaryDeepBlue'
            $fileChanges++
        }
        
        # ========================================
        # استبدال الخطوط
        # ========================================
        
        # Segoe UI -> Cairo
        if ($content -match 'new Font\("Segoe UI", 14, FontStyle\.Bold\)') {
            $content = $content -replace 'new Font\("Segoe UI", 14, FontStyle\.Bold\)', 'ThemeManager.TitleFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 14\)') {
            $content = $content -replace 'new Font\("Segoe UI", 14\)', 'ThemeManager.TitleFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 12, FontStyle\.Bold\)') {
            $content = $content -replace 'new Font\("Segoe UI", 12, FontStyle\.Bold\)', 'ThemeManager.SubtitleFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 12\)') {
            $content = $content -replace 'new Font\("Segoe UI", 12\)', 'ThemeManager.SubtitleFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 10, FontStyle\.Bold\)') {
            $content = $content -replace 'new Font\("Segoe UI", 10, FontStyle\.Bold\)', 'ThemeManager.ButtonFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 10\)') {
            $content = $content -replace 'new Font\("Segoe UI", 10\)', 'ThemeManager.MainFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 9, FontStyle\.Bold\)') {
            $content = $content -replace 'new Font\("Segoe UI", 9, FontStyle\.Bold\)', 'ThemeManager.SmallFont'
            $fileChanges++
        }
        
        if ($content -match 'new Font\("Segoe UI", 9\)') {
            $content = $content -replace 'new Font\("Segoe UI", 9\)', 'ThemeManager.SmallFont'
            $fileChanges++
        }
        
        # ========================================
        # التحقق من الحاجة لإضافة using
        # ========================================
        
        if ($fileChanges -gt 0) {
            # التحقق إذا كان ThemeManager مستخدم بالفعل
            if ($content -match 'ThemeManager\.' -and $content -notmatch 'using FishFarmManager\.Services;') {
                # إضافة using إذا لم يكن موجوداً
                if ($content -match 'using System;') {
                    $content = $content -replace '(using System;)', "`$1`r`nusing FishFarmManager.Services;"
                    Write-Host "  ✅ تمت إضافة: using FishFarmManager.Services" -ForegroundColor Green
                }
            }
        }
        
        # ========================================
        # حفظ التغييرات
        # ========================================
        
        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            $totalChanges += $fileChanges
            $filesModified++
            Write-Host "  ✅ تم التحديث: $fileChanges تغيير" -ForegroundColor Green
        } else {
            Write-Host "  ⏭️  لا توجد تغييرات" -ForegroundColor DarkGray
        }
        
    } catch {
        Write-Host "  ❌ خطأ: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

# ========================================
# التقرير النهائي
# ========================================

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "             التقرير النهائي" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "📊 عدد الملفات المعالجة: $($files.Count)" -ForegroundColor White
Write-Host "✅ عدد الملفات المعدلة: $filesModified" -ForegroundColor Green
Write-Host "🔧 إجمالي التغييرات: $totalChanges" -ForegroundColor Yellow
Write-Host ""

if ($filesModified -gt 0) {
    Write-Host "🎉 اكتمل الاستبدال التلقائي بنجاح!" -ForegroundColor Green
    Write-Host ""
    Write-Host "الخطوات التالية:" -ForegroundColor Yellow
    Write-Host "1. مراجعة التغييرات في Git" -ForegroundColor White
    Write-Host "2. بناء المشروع للتأكد من عدم وجود أخطاء" -ForegroundColor White
    Write-Host "3. اختبار النماذج المعدلة" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 نصيحة: استخدم 'git diff' لمراجعة التغييرات" -ForegroundColor Cyan
} else {
    Write-Host "ℹ️  لم يتم إجراء أي تغييرات" -ForegroundColor Yellow
    Write-Host "جميع الملفات متوافقة بالفعل مع الهوية البصرية" -ForegroundColor White
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan


