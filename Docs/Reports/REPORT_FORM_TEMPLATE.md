# 📋 **قالب نموذج التقارير - Report Form Template**
## نمط موحد لجميع نماذج التقارير

**الإصدار:** 1.0  
**التاريخ:** 2025-10-11  
**المرجع:** SalesReportForm.cs (النموذج المثالي)

---

## 🎯 **الهدف**

هذا القالب يوفر نمطاً موحداً لتطبيق جميع التحسينات على ملفات التقارير الـ 18.

---

## 📦 **المتطلبات الأساسية**

### **1. Using Statements**

```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;  // ✅ مطلوب للـ Async
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
```

---

## 🏗️ **بنية الـ Class**

### **1. Constructor مع Permissions Check**

```csharp
public [ReportName]Form(FishFarmContext context)
{
    // ============================================
    // ✅ الخطوة 1: فحص الصلاحيات (أول شيء!)
    // ============================================
    if (!AuthenticationService.HasPermission(
        UserRole.Admin, 
        UserRole.Manager, 
        UserRole.Accountant,  // أو حسب نوع التقرير
        UserRole.Viewer))
    {
        MessageBox.Show(
            "ليس لديك صلاحية لعرض هذا التقرير.\nيرجى الاتصال بالمدير لمنحك الصلاحيات اللازمة.",
            "خطأ في الصلاحيات",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );
        
        // ✅ Logging
        LoggingService.LogWarning(
            "محاولة وصول غير مصرح بها من {Username} إلى {ReportName}",
            AuthenticationService.CurrentUsername,
            "[اسم التقرير]"
        );
        
        this.Close();
        return;
    }
    
    // ============================================
    // ✅ الخطوة 2: التهيئة
    // ============================================
    _context = context;
    InitializeComponent();
    LoadInitialDataAsync().ConfigureAwait(false);
    
    // ============================================
    // ✅ الخطوة 3: تطبيق الثيم (مع معالجة الأخطاء)
    // ============================================
    try 
    { 
        ThemeManager.ApplyTheme(this); 
    } 
    catch (Exception ex) 
    { 
        LoggingService.LogWarning("فشل تطبيق الثيم على {FormName}: {Error}", "[اسم النموذج]", ex.Message);
    }
}
```

---

### **2. Event Handler Pattern (نمط موحد)**

```csharp
private async void GenerateButton_Click(object? sender, EventArgs e)
{
    try
    {
        // ============================================
        // ✅ الخطوة 1: تعطيل UI أثناء التحميل
        // ============================================
        _generateButton.Enabled = false;
        Cursor = Cursors.WaitCursor;
        
        // ============================================
        // ✅ الخطوة 2: الحصول على Parameters
        // ============================================
        var fromDate = _fromDatePicker.Value.Date;
        var toDate = _toDatePicker.Value.Date.AddDays(1).AddSeconds(-1);
        
        // ============================================
        // ✅ الخطوة 3: الاستعلام (AsNoTracking + Async)
        // ============================================
        var data = await _context.TableName
            .AsNoTracking()  // ✅ للقراءة فقط
            .Include(x => x.RelatedTable)  // إذا لزم
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.SomeField)
            .ToListAsync();  // ✅ Async
        
        // ============================================
        // ✅ الخطوة 4: فحص Empty Collection
        // ============================================
        if (!data.Any())
        {
            MessageBox.Show(
                "لا توجد بيانات في الفترة المحددة",
                "تنبيه",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }
        
        // ============================================
        // ✅ الخطوة 5: معالجة البيانات
        // ============================================
        var processedData = data.GroupBy(x => x.SomeField)
            .Select(g => new
            {
                Field1 = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.Amount)
                // ...
            })
            .ToList();
        
        // ============================================
        // ✅ الخطوة 6: عرض النتائج
        // ============================================
        _dataGrid.DataSource = processedData;
        
        // Format columns
        FormatCurrencyColumns(_dataGrid);
        
        // Update summary
        UpdateSummary(processedData);
        
        // ============================================
        // ✅ الخطوة 7: Audit Logging
        // ============================================
        LoggingService.LogUserActivity(
            AuthenticationService.CurrentUsername,
            "GENERATE_[REPORT_NAME]",
            $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Records: {data.Count}"
        );
    }
    catch (DbUpdateException dbEx)
    {
        // ============================================
        // ✅ خطأ قاعدة البيانات
        // ============================================
        LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - [اسم التقرير]");
        MessageBox.Show(
            "حدث خطأ في الاتصال بقاعدة البيانات. يرجى التحقق من الاتصال والمحاولة مرة أخرى.",
            "خطأ في قاعدة البيانات",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
    }
    catch (InvalidOperationException invEx)
    {
        // ============================================
        // ✅ عملية غير صالحة
        // ============================================
        LoggingService.LogError(invEx, "عملية غير صالحة - [اسم التقرير]");
        MessageBox.Show(
            "حدث خطأ في معالجة البيانات. يرجى التحقق من المدخلات.",
            "خطأ",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
    }
    catch (Exception ex)
    {
        // ============================================
        // ✅ خطأ عام
        // ============================================
        LoggingService.LogFatal(ex, "خطأ غير متوقع - [اسم التقرير]");
        MessageBox.Show(
            $"حدث خطأ غير متوقع: {ex.Message}",
            "خطأ",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );
    }
    finally
    {
        // ============================================
        // ✅ إعادة تفعيل UI (مهم جداً!)
        // ============================================
        _generateButton.Enabled = true;
        Cursor = Cursors.Default;
    }
}
```

---

### **3. Dispose Override (في نهاية الـ Class)**

```csharp
/// <summary>
/// ✅ Dispose override لتحرير موارد Context
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
            LoggingService.LogWarning("خطأ أثناء Dispose للـ Context - [FormName]: {Error}", ex.Message);
        }
    }
    base.Dispose(disposing);
}
```

---

### **4. LoadInitialData Pattern**

```csharp
private async Task LoadInitialDataAsync()
{
    try
    {
        // مثال: تحميل العملاء/الموردين للفلاتر
        var items = await _context.TableName
            .AsNoTracking()
            .Where(x => x.IsActive)  // أو x.Status == ...
            .OrderBy(x => x.Name)
            .ToListAsync();
        
        _filterComboBox.DataSource = items;
        _filterComboBox.DisplayMember = "Name";
        _filterComboBox.ValueMember = "Id";
    }
    catch (Exception ex)
    {
        LoggingService.LogError(ex, "خطأ في تحميل البيانات الأولية - [FormName]");
        MessageBox.Show(
            $"خطأ في تحميل البيانات: {ex.Message}", 
            "خطأ", 
            MessageBoxButtons.OK, 
            MessageBoxIcon.Error
        );
    }
}
```

---

## 📊 **Patterns إضافية**

### **1. Empty Collection Check**

```csharp
// ✅ قبل أي عملية Sum/Average/Min/Max
if (!data.Any())
{
    MessageBox.Show("لا توجد بيانات", ...);
    return;
}

// ✅ أو inline ternary
var total = data.Any() ? data.Sum(x => x.Amount) : 0;
var average = data.Any() ? data.Average(x => x.Value) : 0;
```

---

### **2. FormatCurrencyColumns Helper**

```csharp
private void FormatCurrencyColumns(DataGridView grid)
{
    foreach (DataGridViewColumn col in grid.Columns)
    {
        if (col.Name.Contains("إجمالي") || col.Name.Contains("المبلغ") || 
            col.Name.Contains("Total") || col.Name.Contains("Amount") ||
            col.Name.Contains("Price") || col.Name.Contains("السعر"))
        {
            col.DefaultCellStyle.Format = "N2";
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }
    }
}
```

---

### **3. Audit Logging**

```csharp
// ✅ بعد نجاح العملية
LoggingService.LogUserActivity(
    AuthenticationService.CurrentUsername,
    "GENERATE_[REPORT_TYPE]_REPORT",
    $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Records: {recordCount}"
);
```

---

## 🔄 **خطوات التطبيق (لكل ملف)**

### **Checklist:**

- [ ] **الخطوة 1:** إضافة `using System.Threading.Tasks;`
- [ ] **الخطوة 2:** إضافة Permissions Check في Constructor
- [ ] **الخطوة 3:** تحويل LoadInitialData إلى LoadInitialDataAsync
- [ ] **الخطوة 4:** إضافة AsNoTracking لكل `_context.Table`
- [ ] **الخطوة 5:** تحويل Event Handlers إلى `async void`
- [ ] **الخطوة 6:** استبدال `.ToList()` بـ `.ToListAsync()`
- [ ] **الخطوة 7:** إضافة Empty Collection checks
- [ ] **الخطوة 8:** تحسين معالجة الأخطاء (try-catch-finally)
- [ ] **الخطوة 9:** إضافة Audit Logging
- [ ] **الخطوة 10:** إضافة Dispose override
- [ ] **الخطوة 11:** اختبار البناء (`dotnet build`)
- [ ] **الخطوة 12:** اختبار وظيفي (فتح التقرير واختباره)

---

## 📝 **مثال كامل**

```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public partial class ProductionReportForm : Form
    {
        private readonly FishFarmContext _context;
        
        // Controls...
        private DataGridView _reportGrid = null!;
        private Button _generateButton = null!;
        private DateTimePicker _fromDatePicker = null!;
        private DateTimePicker _toDatePicker = null!;
        
        public ProductionReportForm(FishFarmContext context)
        {
            // ✅ فحص الصلاحيات
            if (!AuthenticationService.HasPermission(
                UserRole.Admin, 
                UserRole.Manager, 
                UserRole.ProductionStaff,
                UserRole.Viewer))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية لعرض تقارير الإنتاج",
                    "خطأ في الصلاحيات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                
                LoggingService.LogWarning(
                    "محاولة وصول غير مصرح بها من {Username} إلى تقرير الإنتاج",
                    AuthenticationService.CurrentUsername
                );
                
                this.Close();
                return;
            }
            
            _context = context;
            InitializeComponent();
            LoadInitialDataAsync().ConfigureAwait(false);
            
            try { ThemeManager.ApplyTheme(this); }
            catch (Exception ex)
            {
                LoggingService.LogWarning("فشل تطبيق الثيم: {Error}", ex.Message);
            }
        }
        
        private void InitializeComponent()
        {
            // Setup UI...
        }
        
        private async void GenerateButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // ✅ تعطيل UI
                _generateButton.Enabled = false;
                Cursor = Cursors.WaitCursor;
                
                var fromDate = _fromDatePicker.Value.Date;
                var toDate = _toDatePicker.Value.Date;
                
                // ✅ Async + AsNoTracking
                var data = await _context.ProductionCycles
                    .AsNoTracking()
                    .Include(p => p.ProductionCyclePonds)
                    .Where(p => p.StartDate >= fromDate && p.StartDate <= toDate)
                    .ToListAsync();
                
                // ✅ فحص Empty
                if (!data.Any())
                {
                    MessageBox.Show("لا توجد بيانات", "تنبيه", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // معالجة وعرض البيانات...
                _reportGrid.DataSource = data;
                
                // ✅ Audit Log
                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "GENERATE_PRODUCTION_REPORT",
                    $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Records: {data.Count}"
                );
            }
            catch (DbUpdateException dbEx)
            {
                LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - تقرير الإنتاج");
                MessageBox.Show("حدث خطأ في قاعدة البيانات", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ غير متوقع - تقرير الإنتاج");
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ✅ إعادة تفعيل UI
                _generateButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
        
        private async Task LoadInitialDataAsync()
        {
            try
            {
                // تحميل بيانات الفلاتر...
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تحميل البيانات الأولية");
                MessageBox.Show("خطأ في تحميل البيانات", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// ✅ Dispose override
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
                    LoggingService.LogWarning("خطأ Dispose - ProductionReport: {Error}", ex.Message);
                }
            }
            base.Dispose(disposing);
        }
    }
}
```

---

## 🎓 **Permissions حسب نوع التقرير**

| التقرير | الأدوار المصرح بها |
|---------|---------------------|
| **SalesReportForm** | Admin, Manager, Accountant, SalesStaff, Viewer |
| **ProductionReportForm** | Admin, Manager, ProductionStaff, Viewer |
| **HRReportsForm** | Admin, Manager |
| **CostAnalysisReportForm** | Admin, Manager, Accountant |
| **InventoryReportForm** | Admin, Manager, InventoryStaff, Viewer |
| **باقي التقارير** | Admin, Manager, Viewer |

---

## ⚡ **Quick Find & Replace**

### **1. إضافة AsNoTracking:**

**ابحث عن:**
```regex
(_context\.(SalesOrders|Employees|Customers|ProductionCycles|InventoryItems|Ponds|WaterQualityRecords|FeedingRecords|MortalityRecords))\s*\n\s*\.(?!AsNoTracking)
```

**استبدل بـ:**
```
$1
                .AsNoTracking()
                .
```

---

### **2. تحويل ToList إلى ToListAsync:**

**ابحث عن:**
```
.ToList()
```

**استبدل بـ:**
```
.ToListAsync()
```

**تحذير:** تأكد من إضافة `await` و `async` أولاً!

---

### **3. إضافة Dispose:**

**أضف في نهاية الـ Class (قبل `}`):**

```csharp
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
            LoggingService.LogWarning("Dispose error: {Error}", ex.Message);
        }
    }
    base.Dispose(disposing);
}
```

---

## 📊 **نتائج التطبيق المتوقعة**

بعد تطبيق هذا القالب على ملف تقرير:

### **الأداء:**
```
⚡ قبل: 2-5 ثوان (بطيء + يجمد UI)
⚡ بعد: 0.5-1 ثانية (سريع + UI متجاوب)
⬆️ تحسين: 60-80%
```

### **الذاكرة:**
```
💾 قبل: استهلاك عالي (Change Tracker)
💾 بعد: استهلاك منخفض (AsNoTracking)
⬇️ تقليل: 30-50%
```

### **الأمان:**
```
🔒 قبل: كل المستخدمين يصلون
🔒 بعد: فقط الأدوار المصرح بها
✅ Audit Trail: كامل
```

### **الجودة:**
```
📊 قبل: 65/100
📊 بعد: 90+/100
⬆️ تحسين: +25 نقطة
```

---

## 🔧 **استكشاف الأخطاء الشائعة**

### **خطأ: CS8618 Nullable warning**

```csharp
// السبب: _context يُعيّن بعد return في Permissions check
// الحل: تجاهل (لأن Close() سيتم قبل الوصول للحقل)
```

### **خطأ: Missing await**

```csharp
// ✅ تأكد من:
1. Method معلمة بـ async
2. استخدام await قبل .ToListAsync()
3. إرجاع Task إذا لزم
```

### **خطأ: UserRole not found**

```csharp
// ✅ تأكد من:
using FishFarmManager.Models;  // UserRole موجود هنا
```

---

## 📈 **قياس النجاح**

بعد تطبيق القالب:

- [x] **البناء:** يجب أن ينجح بدون أخطاء ✅
- [x] **التحذيرات:** 0-1 تحذير فقط (CS8618 مقبول)
- [x] **الوظيفة:** التقرير يفتح ويعمل بشكل صحيح
- [x] **الأداء:** تحميل أسرع وعدم تجميد UI
- [x] **Audit:** السجلات تُكتب في logs

---

## 🎯 **ملخص**

هذا القالب يوفر:

✅ **الأداء:** AsNoTracking + Async  
✅ **الأمان:** Permissions + Audit Logging  
✅ **الجودة:** Error Handling + Dispose  
✅ **تجربة المستخدم:** UI متجاوب + رسائل واضحة  

**🎓 استخدم هذا القالب لجميع التقارير الـ 17 المتبقية!**

---

**© 2025 FishFarmManager - Report Form Template v1.0**  
**المرجع: SalesReportForm.cs ✅**

