# تقرير المشاكل المحتملة - Potential Bugs Report

**التاريخ:** 4 أكتوبر 2025
**الحالة:** قيد المراجعة

## 🐛 المشاكل المُكتشفة من خلال الاختبار

### ✅ المشاكل المُصلحة (6 مشاكل)

#### 1. InventoryManagementForm - عدم حفظ التغييرات ✅

- **السطر:** ~360
- **المشكلة:** مفقود `_context.SaveChanges()` بعد التحديث
- **الحالة:** مُصلح

#### 2. InventoryTransactionsForm - عدم تحديث CurrentStock ✅

- **السطر:** ~450
- **المشكلة:** لا يحدّث `item.CurrentStock` عند إضافة حركة
- **الحالة:** مُصلح

#### 3. StockAlertsForm - عدم حفظ حالة التنبيه ✅

- **السطر:** ResolveButton_Click
- **المشكلة:** مفقود `_context.SaveChanges()`
- **الحالة:** مُصلح

#### 4. InventoryAuditForm - عدم تطبيق تعديلات الجرد ✅

- **السطر:** ~520
- **المشكلة:** لا يطبق التعديلات على `CurrentStock`
- **الحالة:** مُصلح

#### 5. SupplierPaymentForm - InvalidCastException (المحاولة 1) ✅

- **السطر:** 341
- **المشكلة:** cast مباشر من Anonymous Type إلى int
- **الحالة:** مُصلح بإنشاء SupplierItem class

#### 6. SupplierPaymentForm - اسم الخاصية الخاطئ ✅

- **السطر:** 319, 331
- **المشكلة:** استخدام `SupplierId` بدلاً من `Id`
- **الحالة:** مُصلح

---

## ⚠️ المشاكل المحتملة (تحتاج للمراجعة)

### 🔍 مشاكل محتملة في CostRecordForm

#### السطر 677

```csharp
var cycleId = (int)_cycleComboBox.SelectedValue;
```

**المشكلة:** cast مباشر من Anonymous Type
**الحل المقترح:**

```csharp
var cycleId = _cycleComboBox.SelectedValue is int id ? id : 0;
```

#### السطر 680

```csharp
var pondId = (int)_pondComboBox.SelectedValue;
```

**المشكلة:** cast مباشر من Anonymous Type
**الحل المقترح:**

```csharp
var pondId = _pondComboBox.SelectedValue is int id ? id : 0;
```

#### السطر 683

```csharp
var supplierId = (int)_supplierComboBox.SelectedValue;
```

**المشكلة:** cast مباشر من Anonymous Type

**الحل المقترح:**

```csharp
var supplierId = _supplierComboBox.SelectedValue is int id ? id : 0;
```

---

### 🔍 مشاكل محتملة في SalesOrderForm

#### السطر 791

```csharp
int cycleId = (int)_productionCycleComboBox.SelectedValue;
```

**المشكلة:** cast مباشر من Anonymous Type
**يحتاج فحص:** هل LoadProductionCycles تستخدم Anonymous Type؟

#### السطر 824

```csharp
int selectedCycleId = (int)_productionCycleComboBox.SelectedValue;
```

**المشكلة:** نفس المشكلة

#### السطر 1002

```csharp
order.CustomerId = (int)_customerComboBox.SelectedValue;
```

**المشكلة:** cast مباشر
**ملاحظة:** قد لا تسبب مشكلة إذا كان LoadCustomers يستخدم Customer الكامل

---

### 🔍 نماذج أخرى تستخدم Anonymous Type (تحتاج فحص)

1. **TreatmentRecordForm** - السطر 172, 203
2. **FishHealthRecordForm** - السطر 172, 202
3. **EnvironmentalRecordForm** - السطر 170, 185
4. **BatchRecordForm** - السطر 168

**حالة:** تحتاج للفحص والتأكد من عدم وجود InvalidCastException

---

## 📋 خطة الإصلاح المقترحة

### الأولوية 1 (عالية): ✅

- [x] SupplierPaymentForm - مُصلح

### الأولوية 2 (متوسطة): ⏳

- [ ] CostRecordForm - 3 أماكن تحتاج إصلاح
- [ ] SalesOrderForm - 3 أماكن تحتاج فحص وإصلاح محتمل

### الأولوية 3 (منخفضة): ⏳

- [ ] فحص جميع النماذج الأخرى
- [ ] توحيد الأسلوب (استخدام pattern matching في كل مكان)

---

## ✅ الممارسات الجيدة المُطبقة

### الطريقة الصحيحة

```csharp
// 1. إنشاء كلاس helper بسيط
private class ComboItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// 2. استخدامه في Load
var items = _context.Table
    .Select(x => new ComboItem { Id = x.Id, Name = x.Name })
    .ToList();

// 3. استخدام pattern matching آمن
if (_comboBox.SelectedValue is int id && id > 0)
{
    // استخدام id بأمان
}
```

### الطريقة الخاطئة (تسبب أخطاء)

```csharp
// ❌ لا تستخدم Anonymous Type مع ComboBox
var items = _context.Table
    .Select(x => new { x.Id, x.Name })
    .ToList();

// ❌ لا تستخدم cast مباشر
var id = (int)_comboBox.SelectedValue; // InvalidCastException!
```

---

## 📊 ملخص الحالة

- **أخطاء مُكتشفة:** 6
- **أخطاء مُصلحة:** 6 (100%)
- **مشاكل محتملة:** 9+ (تحتاج للفحص)
- **النماذج المُختبرة:** 6 نماذج
- **النماذج المتبقية:** 20+ نموذج

**التوصية:** فحص جميع النماذج التي تستخدم ComboBox مع Anonymous Type وإصلاحها بشكل استباقي قبل ظهور الأخطاء.
