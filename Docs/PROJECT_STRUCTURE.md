# 📁 البنية التنظيمية للمشروع

<div dir="rtl">

## 🎯 نظرة عامة

تم تنظيم المشروع بطريقة احترافية لتسهيل التطوير والصيانة. هذا المستند يشرح البنية التنظيمية الجديدة.

---

## 📊 الإحصائيات

| المجلد | عدد الملفات | الوصف |
|--------|-------------|-------|
| **Docs/Reports/** | 86 | التقارير التقنية والتطويرية |
| **Docs/Guides/** | 9 | الأدلة والوثائق الرئيسية |
| **Docs/Archive/** | 6 | الملفات القديمة والنسخ الاحتياطية |
| **Scripts/** | 4 | سكربتات PowerShell للأتمتة |
| **Logs/** | 8 | ملفات السجلات والبناء |
| **Setup/** | 1 | ملفات الإعداد والتثبيت |
| **Forms/** | 52 | نماذج واجهة المستخدم |
| **Models/** | 42 | نماذج البيانات |
| **Services/** | 7 | خدمات التطبيق |

**إجمالي**: **215 ملف** تم تنظيمها

---

## 🏗️ الهيكل التفصيلي

### 📂 المجلد الجذر

```
FishFarmManagement/
│
├── README.md                    # الدليل الرئيسي للمشروع (جديد)
├── CONTRIBUTING.md             # دليل المساهمة (جديد)
├── .gitignore                  # إعدادات Git (محدث)
│
├── FishFarmManager.sln         # ملف الحل
├── FishFarmManager.csproj      # ملف المشروع
├── Program.cs                  # نقطة الدخول
├── appsettings.json            # إعدادات التطبيق
└── nuget.config                # إعدادات NuGet
```

### 📚 Docs/ - الوثائق

```
Docs/
│
├── 📁 Reports/                 # التقارير التقنية (86 ملف)
│   ├── تقارير البناء (Build Reports)
│   ├── تقارير التقدم (Progress Reports)
│   ├── تقارير الإصلاحات (Fix Reports)
│   ├── تقارير الميزات (Feature Reports)
│   └── تقارير المراجعة (Review Reports)
│
├── 📁 Guides/                  # الأدلة (9 ملفات)
│   ├── دليل_الهوية_البصرية_لـ_AquaFarm_Pro.pdf
│   ├── الدليل_التقني_والتصميمي.md
│   ├── دليل_الوثائق_المحدثة.md
│   ├── UserGuide.md
│   ├── TESTING_GUIDE.md
│   ├── خطة_الطريق_المحدثة_2025.md
│   └── ...
│
├── 📁 Archive/                 # الأرشيف (6 ملفات)
│   ├── ملفات قديمة
│   ├── نسخ احتياطية (.backup)
│   └── نماذج مقترحة
│
└── PROJECT_STRUCTURE.md        # هذا الملف
```

### 🛠️ Scripts/ - السكربتات

```
Scripts/
│
├── quick-rebuild.ps1           # بناء سريع للمشروع
├── fix-visual-identity.ps1     # تطبيق الهوية البصرية
├── fix-reports-auto.ps1        # إصلاح تلقائي للتقارير
└── fix_braces.ps1              # إصلاح الأقواس
```

**الاستخدام:**

```powershell
# من المجلد الجذر
.\Scripts\quick-rebuild.ps1

# أو من مجلد Scripts
cd Scripts
.\quick-rebuild.ps1
```

### 📝 Logs/ - السجلات

```
Logs/
│
├── build_errors.log            # سجل أخطاء البناء
├── build_errors_new.log        # أحدث سجل أخطاء
├── build_output.txt            # مخرجات البناء
├── build_status.txt            # حالة البناء
├── FIX_BUILD_COMMANDS.txt      # أوامر الإصلاح
└── LICENSE.txt                 # رخصة المشروع
```

**ملاحظة**: ملفات السجلات يتم تحديثها تلقائياً عند البناء

### ⚙️ Setup/ - الإعداد

```
Setup/
│
└── FishFarmManagerSetup.iss    # سكربت Inno Setup
```

**الاستخدام**:

```bash
# فتح في Inno Setup Compiler
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Setup\FishFarmManagerSetup.iss
```

### 🖥️ Forms/ - النماذج (52 نموذج)

```
Forms/
│
├── MainForm.cs                 # النموذج الرئيسي
├── LoginForm.cs                # نموذج تسجيل الدخول
├── DashboardForm.cs            # لوحة المعلومات
├── AquaFarmBaseForm.cs         # النموذج الأساسي (ترث منه جميع النماذج)
│
├── 📁 نماذج الإنتاج
│   ├── PondManagementForm.cs
│   ├── ProductionCycleForm.cs
│   ├── BatchRecordForm.cs
│   └── ...
│
├── 📁 نماذج المالية
│   ├── SalesOrderForm.cs
│   ├── CustomerForm.cs
│   ├── SupplierForm.cs
│   └── ...
│
├── 📁 نماذج الموارد البشرية
│   ├── EmployeeForm.cs
│   ├── AttendanceForm.cs
│   ├── SalaryProcessingForm.cs
│   └── ...
│
└── 📁 نماذج التقارير
    ├── ProductionReportForm.cs
    ├── SalesReportForm.cs
    ├── InventoryReportForm.cs
    └── ...
```

### 📊 Models/ - النماذج (42 نموذج)

```
Models/
│
├── User.cs                     # المستخدمون
├── Pond.cs                     # الأحواض
├── ProductionCycle.cs          # دورات الإنتاج
├── Enums.cs                    # التعدادات العامة
│
├── 📁 نماذج الإنتاج
│   ├── BatchRecord.cs
│   ├── FeedingRecord.cs
│   ├── WaterQualityRecord.cs
│   └── ...
│
├── 📁 نماذج المالية
│   ├── SalesOrder.cs
│   ├── SalesOrderItem.cs
│   ├── Customer.cs
│   ├── CustomerPayment.cs
│   └── ...
│
├── 📁 نماذج الموارد البشرية
│   ├── Employee.cs
│   ├── Attendance.cs
│   ├── Salary.cs
│   └── ...
│
└── 📁 نماذج الجودة
    ├── QualityTest.cs
    ├── Certification.cs
    ├── HACCPRecord.cs
    └── ...
```

### ⚙️ Services/ - الخدمات (7 خدمات)

```
Services/
│
├── AuthenticationService.cs    # خدمة المصادقة
├── AuditService.cs             # خدمة التدقيق
├── DatabaseService.cs          # خدمة قاعدة البيانات
├── LoggingService.cs           # خدمة السجلات
├── ReportService.cs            # خدمة التقارير
├── ExportService.cs            # خدمة التصدير
└── ValidationService.cs        # خدمة التحقق
```

### 🗄️ Data/ - البيانات

```
Data/
│
├── FishFarmContext.cs          # DbContext الرئيسي
└── DataSeeder.cs               # بيانات أولية للاختبار
```

### 🔄 Migrations/ - الهجرات

```
Migrations/
│
├── 20251007152559_InitialCreate.cs
├── 20251007152559_InitialCreate.Designer.cs
├── 20251008080013_AddAuthenticationSystem.cs
├── 20251008080013_AddAuthenticationSystem.Designer.cs
└── FishFarmContextModelSnapshot.cs
```

### 🎨 Assets/ - الأصول

```
Assets/
│
├── Icons/                      # الأيقونات
│   ├── app-icon.ico
│   └── ...
│
├── Images/                     # الصور
│   ├── logo.png
│   ├── splash.png
│   └── ...
│
└── README_LOGO.md              # وصف الشعارات
```

### 🎛️ Controls/ - العناصر المخصصة

```
Controls/
│
├── ChartControl.cs             # عنصر الرسوم البيانية
└── SmartAutoCompleteTextBox.cs # صندوق نص ذكي
```

### 🧪 Tests/ - الاختبارات

```
Tests/
│
├── UnitTests/                  # اختبارات الوحدة
├── IntegrationTests/           # اختبارات التكامل
└── TestData/                   # بيانات اختبار
```

---

## 🎯 التصنيف حسب الوظيفة

### 📝 التقارير في Docs/Reports/

#### 1. تقارير البناء والإصلاحات

- `BUILD_SUCCESS_REPORT_*.md` - تقارير البناء الناجح
- `BUILD_ERRORS_ANALYSIS.md` - تحليل أخطاء البناء
- `BUG_FIX_REPORT.md` - تقارير إصلاح الأخطاء
- `CRITICAL_FIXES_NOW.md` - الإصلاحات الحرجة

#### 2. تقارير التقدم

- `WEEK*_*.md` - تقارير أسبوعية
- `PHASE*_*.md` - تقارير المراحل
- `PROGRESS_REPORT*.md` - تقارير التقدم العامة
- `SESSION_SUMMARY_*.md` - ملخصات الجلسات

#### 3. تقارير الميزات

- `AUTHENTICATION_SYSTEM_REPORT.md` - نظام المصادقة
- `AUTOCOMPLETE_IMPLEMENTATION_REPORT.md` - الإكمال التلقائي
- `LOGGING_SYSTEM_REPORT.md` - نظام السجلات
- `INVENTORY_COMPLETION_REPORT.md` - نظام المخزون
- `PURCHASING_DAY1_REPORT.md` - نظام المشتريات

#### 4. تقارير المراجعة

- `CODE_REVIEW_AND_FIXES_REPORT.md` - مراجعة الكود
- `COMPREHENSIVE_AUDIT_REPORT.md` - التدقيق الشامل
- `DOCUMENTATION_REVIEW_REPORT.md` - مراجعة الوثائق

#### 5. تقارير الهوية البصرية (عربي)

- `تقرير_تنفيذ_الهوية_البصرية_AquaFarm_Pro.md`
- `تقرير_إكمال_المرحلة_الأولى_الهوية_البصرية.md`
- `ملخص_تنفيذ_الهوية_البصرية.md`

### 📚 الأدلة في Docs/Guides/

#### 1. الأدلة الفنية

- `الدليل_التقني_والتصميمي.md` - دليل تقني شامل
- `DEVELOPER_GUIDE_MEMORY_MANAGEMENT.md` - إدارة الذاكرة

#### 2. أدلة المستخدم

- `UserGuide.md` - دليل المستخدم العام
- `SALES_USER_GUIDE.md` - دليل نظام المبيعات
- `TESTING_GUIDE.md` - دليل الاختبار

#### 3. الخطط والاستراتيجيات

- `خطة_الطريق_المحدثة_2025.md` - خارطة الطريق
- `خطة_التنفيذ_التفصيلية.md` - خطة التنفيذ

#### 4. الهوية البصرية

- `دليل_الهوية_البصرية_لـ_AquaFarm_Pro.pdf` - دليل شامل PDF
- `جدول_مرجعي_سريع_الهوية_البصرية.md` - مرجع سريع

---

## 🔍 البحث والملاحة

### البحث عن ملفات معينة

#### التقارير

```powershell
# البحث في التقارير
Get-ChildItem Docs/Reports/ -Filter "*BUILD*"
Get-ChildItem Docs/Reports/ -Filter "*WEEK*"
Get-ChildItem Docs/Reports/ -Filter "*تقرير*"
```

#### النماذج

```powershell
# البحث في النماذج
Get-ChildItem Forms/ -Filter "*Report*"
Get-ChildItem Forms/ -Filter "*Form.cs"
```

#### الخدمات

```powershell
# البحث في الخدمات
Get-ChildItem Services/ -Filter "*Service.cs"
```

### فتح مجلدات محددة

```powershell
# فتح مجلد التقارير
explorer Docs\Reports\

# فتح مجلد الأدلة
explorer Docs\Guides\

# فتح مجلد النماذج
explorer Forms\
```

---

## 📋 قواعد التنظيم

### ما يجب أن يكون في المجلد الجذر

✅ **يُسمح**:

- ملفات الحل (.sln)
- ملفات المشروع (.csproj)
- ملفات الإعداد (appsettings.json, nuget.config)
- ملف البرنامج الرئيسي (Program.cs)
- ملفات التوثيق الأساسية (README.md, CONTRIBUTING.md, .gitignore)

❌ **غير مسموح**:

- تقارير (.md تفصيلية)
- سجلات (.log, .txt)
- سكربتات (.ps1)
- نسخ احتياطية (.backup)
- ملفات مؤقتة

### أين تضع الملفات الجديدة

| نوع الملف | المجلد |
|-----------|--------|
| **تقرير تقني** | `Docs/Reports/` |
| **دليل أو وثيقة** | `Docs/Guides/` |
| **سكربت PowerShell** | `Scripts/` |
| **سجل أو log** | `Logs/` |
| **ملف قديم** | `Docs/Archive/` |
| **نموذج UI** | `Forms/` |
| **نموذج بيانات** | `Models/` |
| **خدمة** | `Services/` |

---

## 🧹 الصيانة

### تنظيف دوري

```powershell
# حذف ملفات البناء المؤقتة
Remove-Item bin\*, obj\* -Recurse -Force

# حذف سجلات قديمة (أكثر من 30 يوم)
Get-ChildItem Logs\*.log | Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} | Remove-Item

# نقل تقارير قديمة إلى الأرشيف
Move-Item Docs\Reports\WEEK*.md Docs\Archive\ -Force
```

### أرشفة دورية

يُنصح بأرشفة التقارير القديمة كل شهر:

```powershell
# إنشاء أرشيف شهري
$month = (Get-Date).ToString("yyyy-MM")
New-Item -ItemType Directory -Path "Docs\Archive\$month"
Move-Item Docs\Reports\*$month*.md "Docs\Archive\$month\"
```

---

## 📊 الخلاصة

### قبل التنظيم

- ✗ **90+ ملف** في المجلد الجذر
- ✗ صعوبة في الملاحة والبحث
- ✗ بنية غير احترافية

### بعد التنظيم

- ✓ **6 ملفات فقط** في المجلد الجذر
- ✓ تصنيف منطقي ومنظم
- ✓ سهولة الوصول والصيانة
- ✓ بنية احترافية وقابلة للتوسع

---

## 🎯 التوصيات

1. **التزم بالبنية**: احرص على وضع الملفات في المجلدات الصحيحة
2. **التسمية الموحدة**: استخدم نفس نمط التسمية للملفات المتشابهة
3. **التوثيق**: وثّق أي تغييرات كبيرة في البنية
4. **الأرشفة**: انقل الملفات القديمة إلى Archive دورياً
5. **النظافة**: احذف الملفات المؤقتة والسجلات القديمة

---

**آخر تحديث**: 13 أكتوبر 2025

</div>
