# 🤝 دليل المساهمة في AquaFarm Pro

<div dir="rtl">

## 🎯 مرحباً بك

نشكرك على اهتمامك بالمساهمة في **AquaFarm Pro**. هذا الدليل يساعدك على فهم كيفية المساهمة بفعالية في المشروع.

---

## 📋 جدول المحتويات

1. [قواعد السلوك] (#قواعد-السلوك)
2. [كيفية المساهمة] (#كيفية-المساهمة)
3. [معايير الكود] (#معايير-الكود)
4. [عملية المراجعة] (#عملية-المراجعة)
5. [الإبلاغ عن الأخطاء] (#الإبلاغ-عن-الأخطاء)
6. [اقتراح ميزات جديدة] (#اقتراح-ميزات-جديدة)

---

## 🤝 قواعد السلوك

### المبادئ الأساسية

- **الاحترام**: تعامل مع الجميع باحترام ومهنية
- **التعاون**: نحن فريق واحد نعمل معاً
- **الجودة**: نسعى دائماً للتميز
- **الشفافية**: كن واضحاً في تواصلك

---

## 🚀 كيفية المساهمة

### 1. تجهيز البيئة

```bash
# استنساخ المشروع
git clone [repository-url]
cd FishFarmManagement

# إنشاء فرع جديد
git checkout -b feature/your-feature-name

# استعادة الحزم
dotnet restore

# تحديث قاعدة البيانات
dotnet ef database update
```

### 2. البدء بالعمل

#### أنواع المساهمات

- 🐛 **إصلاح الأخطاء** (Bug Fixes)
- ✨ **ميزات جديدة** (Features)
- 📝 **تحسين الوثائق** (Documentation)
- 🎨 **تحسينات واجهة المستخدم** (UI/UX)
- ⚡ **تحسين الأداء** (Performance)
- ♻️ **إعادة الهيكلة** (Refactoring)

#### تسمية الفروع

```bash
# إصلاح خطأ
git checkout -b fix/bug-description

# ميزة جديدة
git checkout -b feature/feature-name

# تحسين
git checkout -b improvement/improvement-name

# توثيق
git checkout -b docs/documentation-update
```

### 3. إجراء التغييرات

#### قائمة التحقق قبل الالتزام

- [ ] الكود يتبع معايير المشروع
- [ ] تم اختبار التغييرات محلياً
- [ ] لا توجد أخطاء بناء
- [ ] تم تحديث الوثائق (إن لزم الأمر)
- [ ] تم إضافة اختبارات (للميزات الجديدة)
- [ ] الكود معلق بشكل مناسب

### 4. الالتزام (Commit)

#### تنسيق رسائل الالتزام

```
<type>(<scope>): <subject>

<body>

<footer>
```

#### أنواع الالتزامات

- `feat`: ميزة جديدة
- `fix`: إصلاح خطأ
- `docs`: تحديث وثائق
- `style`: تنسيق الكود (بدون تغيير منطقي)
- `refactor`: إعادة هيكلة
- `perf`: تحسين أداء
- `test`: إضافة اختبارات
- `chore`: مهام صيانة

#### أمثلة

```bash
# ميزة جديدة
git commit -m "feat(sales): إضافة تقرير المبيعات الشهري"

# إصلاح خطأ
git commit -m "fix(inventory): إصلاح حساب الكميات المتاحة"

# تحديث وثائق
git commit -m "docs(readme): تحديث تعليمات التثبيت"
```

### 5. إرسال Pull Request

```bash
# رفع التغييرات
git push origin feature/your-feature-name
```

#### محتوى Pull Request

- **العنوان**: عنوان واضح ومختصر
- **الوصف**: شرح تفصيلي للتغييرات
- **المشكلة**: ما المشكلة التي تحلها؟
- **الحل**: كيف حللتها؟
- **الاختبار**: كيف تم اختبار التغييرات؟
- **لقطات شاشة**: إن كانت تغييرات UI

#### قالب Pull Request

```markdown
## 📝 الوصف
وصف موجز للتغييرات

## 🎯 المشكلة
Issue #123 - وصف المشكلة

## ✅ الحل
- شرح كيفية حل المشكلة
- التغييرات الرئيسية

## 🧪 الاختبار
- [ ] تم الاختبار محلياً
- [ ] جميع الاختبارات تمر بنجاح
- [ ] تم اختبار السيناريوهات المختلفة

## 📸 لقطات الشاشة
(إن وجدت)

## 📋 قائمة التحقق
- [ ] الكود يتبع معايير المشروع
- [ ] لا توجد أخطاء بناء
- [ ] تم تحديث الوثائق
- [ ] تمت إضافة اختبارات
```

---

## 💻 معايير الكود

### 1. اصطلاحات التسمية

#### C# Naming Conventions

```csharp
// Classes - PascalCase
public class CustomerPaymentForm { }

// Methods - PascalCase
public void CalculateTotalAmount() { }

// Properties - PascalCase
public string CustomerName { get; set; }

// Fields (private) - camelCase مع _
private readonly ILogger _logger;
private string _connectionString;

// Constants - PascalCase
public const string DefaultCurrency = "SAR";

// Interfaces - PascalCase with I prefix
public interface IPaymentService { }
```

#### قاعدة البيانات

```csharp
// Table Names - Singular, PascalCase
public class Customer { }

// Column Names - PascalCase
public string FirstName { get; set; }

// Foreign Keys - SingularTableName + "Id"
public int CustomerId { get; set; }
```

### 2. بنية الملفات

```csharp
// File Structure Template
using System;
using System.Linq;
// ... other usings (alphabetically)

namespace FishFarmManagement.Forms
{
    /// <summary>
    /// وصف مختصر للكلاس
    /// </summary>
    public class CustomerForm : AquaFarmBaseForm
    {
        #region Fields
        
        private readonly ILogger<CustomerForm> _logger;
        private readonly FishFarmContext _context;
        
        #endregion

        #region Constructor
        
        public CustomerForm()
        {
            InitializeComponent();
            InitializeForm();
        }
        
        #endregion

        #region Properties
        
        public int CustomerId { get; set; }
        
        #endregion

        #region Event Handlers
        
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Implementation
        }
        
        #endregion

        #region Private Methods
        
        private void LoadData()
        {
            // Implementation
        }
        
        #endregion

        #region Override Methods
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Custom implementation
        }
        
        #endregion
    }
}
```

### 3. التعليقات والتوثيق

```csharp
/// <summary>
/// حساب إجمالي تكلفة الطلب شاملاً الضريبة
/// </summary>
/// <param name="subtotal">المبلغ قبل الضريبة</param>
/// <param name="taxRate">نسبة الضريبة (0.15 = 15%)</param>
/// <returns>المبلغ الإجمالي شامل الضريبة</returns>
public decimal CalculateTotalWithTax(decimal subtotal, decimal taxRate)
{
    // التحقق من صحة المدخلات
    if (subtotal < 0)
        throw new ArgumentException("المبلغ لا يمكن أن يكون سالباً", nameof(subtotal));
    
    if (taxRate < 0 || taxRate > 1)
        throw new ArgumentException("نسبة الضريبة يجب أن تكون بين 0 و 1", nameof(taxRate));
    
    // حساب الضريبة والإجمالي
    var taxAmount = subtotal * taxRate;
    var total = subtotal + taxAmount;
    
    return Math.Round(total, 2); // تقريب إلى منزلتين عشريتين
}
```

### 4. معالجة الأخطاء

```csharp
public async Task<bool> SaveCustomerAsync(Customer customer)
{
    try
    {
        // Validation
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));
        
        // Business logic
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        
        // Logging
        _logger.LogInformation("تم حفظ العميل {CustomerId} بنجاح", customer.CustomerId);
        
        return true;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "خطأ في حفظ بيانات العميل");
        MessageBox.Show("حدث خطأ في حفظ البيانات", "خطأ", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "خطأ غير متوقع");
        throw; // Re-throw for critical errors
    }
}
```

### 5. الهوية البصرية

```csharp
// استخدام الألوان الموحدة
public static class BrandColors
{
    public static readonly Color PrimaryBlue = Color.FromArgb(46, 92, 138);     // #2E5C8A
    public static readonly Color LightBlue = Color.FromArgb(74, 144, 226);      // #4A90E2
    public static readonly Color AquaGreen = Color.FromArgb(38, 166, 154);      // #26A69A
    public static readonly Color WarningOrange = Color.FromArgb(255, 152, 0);   // #FF9800
    public static readonly Color DangerRed = Color.FromArgb(229, 57, 53);       // #E53935
}

// استخدام الخط الموحد
this.Font = new Font("Cairo", 10F);
this.RightToLeft = RightToLeft.Yes;
this.RightToLeftLayout = true;
```

---

## 🔍 عملية المراجعة

### ما نبحث عنه

1. **الوظيفة**: هل الكود يعمل كما هو متوقع؟
2. **الجودة**: هل الكود نظيف وقابل للصيانة؟
3. **الأداء**: هل هناك مشاكل أداء محتملة؟
4. **الأمان**: هل هناك ثغرات أمنية؟
5. **الاتساق**: هل يتبع معايير المشروع؟
6. **الاختبار**: هل تم اختبار الكود بشكل كافٍ؟

### معايير القبول

- ✅ جميع الاختبارات تمر بنجاح
- ✅ لا توجد أخطاء بناء
- ✅ الكود يتبع المعايير
- ✅ تم تحديث الوثائق
- ✅ موافقة اثنين من المراجعين (لتغييرات كبيرة)

---

## 🐛 الإبلاغ عن الأخطاء

### قبل الإبلاغ

- [ ] تأكد من أن المشكلة لم يتم الإبلاغ عنها مسبقاً
- [ ] استخدم أحدث نسخة من المشروع
- [ ] حاول إعادة إنتاج المشكلة

### قالب الإبلاغ

```markdown
## 🐛 وصف المشكلة
وصف واضح ومختصر للمشكلة

## 📋 خطوات إعادة الإنتاج
1. اذهب إلى '...'
2. انقر على '...'
3. شاهد الخطأ

## ✅ السلوك المتوقع
ما كان يجب أن يحدث

## ❌ السلوك الفعلي
ما حدث بالفعل

## 🖼️ لقطات الشاشة
(إن وجدت)

## 🔧 البيئة
- نظام التشغيل: Windows 11
- إصدار .NET: 8.0
- إصدار التطبيق: 1.0.0
- قاعدة البيانات: SQL Server 2022

## 📝 معلومات إضافية
أي سياق إضافي حول المشكلة
```

---

## ✨ اقتراح ميزات جديدة

### قبل الاقتراح

- [ ] تأكد من أن الميزة لم يتم اقتراحها مسبقاً
- [ ] تأكد من أن الميزة تتوافق مع رؤية المشروع

### قالب الاقتراح

```markdown
## 💡 وصف الميزة
وصف واضح للميزة المقترحة

## 🎯 المشكلة التي تحلها
لماذا نحتاج هذه الميزة؟

## 🔧 الحل المقترح
كيف يمكن تنفيذ هذه الميزة؟

## 🔀 بدائل أخرى
هل هناك حلول بديلة؟

## 📊 التأثير
- على الأداء
- على المستخدمين
- على الكود الحالي

## 📋 المتطلبات
- [ ] متطلب 1
- [ ] متطلب 2
```

---

## 📚 الموارد المفيدة

### الوثائق

- [README.md](README.md) - نظرة عامة على المشروع
- [Docs/Guides/](Docs/Guides/) - الأدلة التقنية
- [Docs/Reports/](Docs/Reports/) - التقارير التقنية

### أدوات التطوير

- **Visual Studio 2022** أو أحدث
- **SQL Server Management Studio**
- **Git** للتحكم في الإصدارات
- **Inno Setup** لإنشاء ملفات التثبيت

### موارد خارجية

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Entity Framework Core Docs](https://docs.microsoft.com/en-us/ef/core/)
- [Windows Forms Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)

---

## 🙏 شكراً لك

مساهمتك تجعل **AquaFarm Pro** أفضل للجميع. نقدر وقتك وجهدك! 💙

---

**إذا كان لديك أي أسئلة، لا تتردد في طرحها!**

</div>
