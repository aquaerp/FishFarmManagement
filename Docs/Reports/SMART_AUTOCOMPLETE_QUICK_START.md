# 🚀 **دليل البدء السريع - نظام الإكمال الذكي**

## Smart Autocomplete System - Quick Start Guide

---

## 📦 **الملفات الأساسية**

```text
Services/
  └── AutocompleteService.cs          # الخدمة الأساسية

Controls/
  └── SmartAutoCompleteTextBox.cs     # مكون الواجهة

Forms/
  ├── CustomerForm.cs                 # مثال: العملاء ✅
  ├── SupplierForm.cs                 # مثال: الموردين ✅
  └── InventoryItemForm.cs            # مثال: المخزون ✅
```

---

## ⚡ **البدء السريع (5 دقائق)**

### **1. إضافة المراجع**

```csharp
using FishFarmManager.Controls;
using FishFarmManager.Services;
```

### **2. إنشاء الخدمة**

```csharp
private readonly AutocompleteService _autocompleteService;

public YourForm(FishFarmContext context)
{
    _context = context;
    _autocompleteService = new AutocompleteService(_context);
    InitializeComponent();
}
```

### **3. استبدال TextBox**

```csharp
// القديم:
private TextBox _nameTextBox;

// الجديد:
private SmartAutoCompleteTextBox _nameTextBox;
```

### **4. التهيئة**

```csharp
_nameTextBox = new SmartAutoCompleteTextBox(_autocompleteService)
{
    Location = new Point(100, 50),
    Width = 300,
    EntityType = "Customer",    // 👈 اختر: Customer, Supplier, InventoryItem
    SearchField = "All"
};
_nameTextBox.DataFilled += OnDataFilled;  // 👈 معالج التعبئة التلقائية
```

### **5. معالج التعبئة التلقائية**

```csharp
private void OnDataFilled(object? sender, AutocompleteResult e)
{
    if (e.AdditionalData.ContainsKey("Phone"))
        _phoneTextBox.Text = e.AdditionalData["Phone"]?.ToString() ?? "";
    
    if (e.AdditionalData.ContainsKey("Email"))
        _emailTextBox.Text = e.AdditionalData["Email"]?.ToString() ?? "";
    
    // ... تعبئة المزيد من الحقول
}
```

---

## 🎨 **أنواع الكيانات المدعومة**

| EntityType | الوصف | الحقول القابلة للبحث |
|-----------|-------|---------------------|
| `Customer` | العملاء | الاسم، الهاتف، البريد، الرقم الضريبي |
| `Supplier` | الموردين | الاسم، جهة الاتصال، الهاتف، البريد |
| `InventoryItem` | الأصناف | الاسم، الكود، الباركود، الوصف |
| `Employee` | الموظفين | الاسم، الهاتف |
| `Equipment` | المعدات | الاسم، الرقم التسلسلي |
| `Pond` | الأحواض | الاسم، الوصف |

---

## ⚙️ **خيارات التخصيص**

```csharp
smartTextBox.MinCharacters = 2;         // الحد الأدنى للبحث (افتراضي: 2)
smartTextBox.SearchDelayMs = 300;       // التأخير بالمللي ثانية (افتراضي: 300)
smartTextBox.AutoFillDetails = true;    // تعبئة تلقائية (افتراضي: true)
smartTextBox.RecordUsage = true;        // تسجيل الاستخدام (افتراضي: true)
smartTextBox.SearchField = "All";       // البحث في كل الحقول
// أو: "Name", "Phone", "Email"
```

---

## 🎯 **نماذج جاهزة (Copy & Paste)**

### **نموذج 1: البحث عن عميل**

```csharp
var customerSearch = new SmartAutoCompleteTextBox(_autocompleteService)
{
    EntityType = "Customer",
    SearchField = "All"
};
customerSearch.DataFilled += (s, e) =>
{
    MessageBox.Show($"تم اختيار: {e.DisplayText}");
};
```

### **نموذج 2: البحث عن صنف مخزون**

```csharp
var itemSearch = new SmartAutoCompleteTextBox(_autocompleteService)
{
    EntityType = "InventoryItem",
    SearchField = "All",
    MinCharacters = 3  // 3 أحرف للأصناف
};
itemSearch.ItemSelected += (s, e) =>
{
    decimal stock = e.SelectedItem.GetAdditionalData<decimal>("CurrentStock");
    MessageBox.Show($"المخزون المتاح: {stock}");
};
```

---

## 💡 **نصائح مهمة**

### ✅ **افعل**

- استخدم `SearchField = "All"` للبحث الشامل
- اربط حدث `DataFilled` للتعبئة التلقائية
- اختبر مع 10+ سجلات في قاعدة البيانات
- استخدم `ClearSelection()` بدلاً من `Clear()`

### ❌ **لا تفعل**

- لا تستخدم `MinCharacters = 1` (بطيء)
- لا تنسى تعيين `EntityType`
- لا تستخدم `Clear()` (استخدم `ClearSelection()`)
- لا تحذف معالج `DataFilled` إذا كنت تريد التعبئة التلقائية

---

## 🔧 **حل المشاكل السريع**

### **المشكلة: القائمة لا تظهر**

```csharp
// تحقق من:
1. EntityType محدد؟
2. MinCharacters <= عدد الأحرف المدخلة؟
3. يوجد بيانات في الجدول؟
```

### **المشكلة: بطء في الأداء**

```csharp
// زد التأخير:
smartTextBox.SearchDelayMs = 500;  // بدلاً من 300
```

### **المشكلة: البيانات لا تُعبأ**

```csharp
// تأكد من:
smartTextBox.AutoFillDetails = true;
smartTextBox.DataFilled += YourHandler;  // معالج مربوط
```

---

## 📚 **موارد إضافية**

- 📖 [التوثيق الكامل](SMART_AUTOCOMPLETE_DOCUMENTATION.md)
- 💻 [أمثلة تطبيقية](Forms/CustomerForm.cs)
- 🎓 [دليل المطور المتقدم] (#)

---

## 🎉 **مثال كامل ومتكامل**

```csharp
using System;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Controls;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public partial class MyForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AutocompleteService _autocompleteService;
        
        private SmartAutoCompleteTextBox _customerSearch;
        private TextBox _phoneTextBox;
        private TextBox _emailTextBox;
        
        public MyForm(FishFarmContext context)
        {
            _context = context;
            _autocompleteService = new AutocompleteService(_context);
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // مربع البحث الذكي
            _customerSearch = new SmartAutoCompleteTextBox(_autocompleteService)
            {
                Location = new Point(100, 50),
                Width = 300,
                EntityType = "Customer",
                SearchField = "All",
                MinCharacters = 2,
                SearchDelayMs = 300,
                AutoFillDetails = true,
                RecordUsage = true
            };
            _customerSearch.DataFilled += CustomerSearch_DataFilled;
            this.Controls.Add(_customerSearch);
            
            // حقل الهاتف
            _phoneTextBox = new TextBox
            {
                Location = new Point(100, 90),
                Width = 200
            };
            this.Controls.Add(_phoneTextBox);
            
            // حقل البريد
            _emailTextBox = new TextBox
            {
                Location = new Point(100, 130),
                Width = 200
            };
            this.Controls.Add(_emailTextBox);
        }
        
        private void CustomerSearch_DataFilled(object? sender, AutocompleteResult e)
        {
            try
            {
                // تعبئة تلقائية للحقول
                if (e.AdditionalData.ContainsKey("Phone"))
                    _phoneTextBox.Text = e.AdditionalData["Phone"]?.ToString() ?? "";
                
                if (e.AdditionalData.ContainsKey("Email"))
                    _emailTextBox.Text = e.AdditionalData["Email"]?.ToString() ?? "";
                
                MessageBox.Show(
                    $"تم اختيار العميل: {e.DisplayText}",
                    "نجح",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ: {ex.Message}",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
```

---

**🎓 نصيحة للمبتدئين:**  
ابدأ بنسخ المثال الكامل أعلاه، ثم عدّله حسب احتياجاتك!

**⏱️ وقت التطبيق المتوقع:** 5-10 دقائق للنموذج الأول

---

**© 2025 FishFarmManager - نظام الإكمال الذكي**
