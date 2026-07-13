# خطة الأسبوع 7 - نظام المشتريات

 Week 7 Plan - Purchasing System

**التاريخ:** 7-13 أكتوبر 2025  
**الهدف:** إكمال نظام المشتريات بالكامل  
**الجهد المتوقع:** ~2,800 سطر برمجي  
**نسبة الإنجاز المستهدفة:** 75% (من 68% حالياً)

---

## 📋 نظرة عامة

نظام المشتريات هو نظام شامل لإدارة:

- طلبات الشراء (Purchase Orders)
- استلام المشتريات (Receiving)
- تقارير المشتريات (Reports)
- تكامل مع المخزون والموردين

---

## 🎯 المهام الرئيسية

### اليوم 1-2: النماذج (Models) + Migration

#### 1. PurchaseOrder.cs (~200 سطر)

**المتطلبات:**

```csharp
- OrderNumber (string, unique, auto-generated)
- OrderDate (DateTime)
- ExpectedDeliveryDate (DateTime)
- SupplierId (FK -> Supplier)
- Status (enum: Draft/Pending/Approved/PartiallyReceived/Received/Cancelled)
- SubTotal (decimal)
- VATRate (15%)
- VATAmount (decimal, calculated)
- Total (decimal, calculated)
- DiscountAmount (decimal)
- PaymentTerms (string)
- DeliveryAddress (string)
- Notes (string)
- CreatedBy (FK -> Employee)
- CreatedAt (DateTime)
- ApprovedBy (FK -> Employee)
- ApprovedAt (DateTime?)
- RequestedBy (FK -> Employee)
- Department (string)

// Calculated Properties
- IsOverdue
- DaysUntilDelivery
- ReceivingProgress (%)

// Collections
- PurchaseOrderItems (List)
```

**الحسابات:**

- SubTotal = Sum(Item.Quantity * Item.UnitPrice)
- VATAmount = SubTotal * 0.15
- Total = SubTotal + VATAmount - DiscountAmount

**6 حالات للطلب:**

1. Draft - مسودة
2. Pending - معلق
3. Approved - معتمد
4. PartiallyReceived - مستلم جزئياً
5. Received - مستلم بالكامل
6. Cancelled - ملغي

#### 2. PurchaseOrderItem.cs (~120 سطر)

**المتطلبات:**

```csharp
- PurchaseOrderId (FK -> PurchaseOrder)
- InventoryItemId (FK -> InventoryItem)
- Quantity (decimal)
- UnitPrice (decimal)
- DiscountPercentage (decimal)
- DiscountAmount (decimal, calculated)
- SubTotal (decimal, calculated)
- ReceivedQuantity (decimal)
- RemainingQuantity (decimal, calculated)
- Notes (string)

// Calculated Properties
- LineTotal = (Quantity * UnitPrice) - DiscountAmount
- IsFullyReceived
- ReceivingProgress (%)
```

#### 3. Migration

**الجداول:**

- PurchaseOrders
- PurchaseOrderItems

**العلاقات:**

- PurchaseOrder -> Supplier (Many-to-One)
- PurchaseOrder -> Employee (Many-to-One, CreatedBy)
- PurchaseOrder -> Employee (Many-to-One, ApprovedBy)
- PurchaseOrder -> PurchaseOrderItems (One-to-Many)
- PurchaseOrderItem -> InventoryItem (Many-to-One)

**الوقت:** 8 ساعات (يوم واحد)

---

### اليوم 3-4: PurchaseOrderForm.cs (~900 سطر)

**الهيكل:**

```text
TabControl:
  1. تفاصيل الطلب (Order Details)
  2. قائمة الطلبات (Orders List)
```

*Tab 1: تفاصيل الطلب**

**القسم العلوي:**

- رقم الطلب (auto-generated: PO-YYYYMMDD-001)
- تاريخ الطلب (DateTimePicker)
- تاريخ التسليم المتوقع (DateTimePicker)
- المورد (ComboBox)
- الحالة (ComboBox, read-only)
- طالب الطلب (ComboBox - Employee)
- القسم (TextBox)

**قسم البنود (DataGridView):**

```text
Columns:
- البند (ComboBox - InventoryItem)
- الوحدة (Label, auto-fill)
- الكمية (NumericUpDown)
- سعر الوحدة (NumericUpDown)
- خصم % (NumericUpDown)
- خصم ريال (calculated)
- الإجمالي (calculated)
- [حذف] (Button)
```

**أزرار البنود:**

- [إضافة بند]
- [حذف بند محدد]
- [مسح كل البنود]

**قسم الحسابات (Panel - Left):**

```text
المجموع الفرعي: XXX.XX ريال
خصم الطلب: XXX.XX ريال
المجموع بعد الخصم: XXX.XX ريال
ضريبة القيمة المضافة (15%): XXX.XX ريال
───────────────────────────
الإجمالي النهائي: XXX.XX ريال
```

**قسم التفاصيل الإضافية:**

- شروط الدفع (TextBox)
- عنوان التسليم (TextBox)
- ملاحظات (TextBox, multiline)

**الأزرار:**

- [جديد] - طلب جديد
- [حفظ كمسودة] - حفظ دون اعتماد
- [اعتماد وحفظ] - اعتماد الطلب
- [تعديل] - تعديل طلب موجود
- [إلغاء الطلب] - إلغاء طلب
- [إلغاء التعديلات] - Cancel
- [طباعة] - طباعة الطلب

*Tab 2: قائمة الطلبات**

**الفلاتر:**

- المورد (ComboBox)
- الحالة (ComboBox: الكل, مسودة, معلق, معتمد, مستلم جزئياً, مستلم, ملغي)
- من تاريخ (DateTimePicker)
- إلى تاريخ (DateTimePicker)
- بحث (TextBox: رقم الطلب، البند)

**DataGridView:**

```text
Columns:
- رقم الطلب
- التاريخ
- المورد
- المجموع الفرعي
- الضريبة
- الإجمالي
- الحالة
- المستلم %
- متأخر؟
```

**الأزرار:**

- [تحديث] - Refresh
- [تصدير Excel]
- [تصدير PDF]
- [عرض التفاصيل] - عرض الطلب المحدد

**الوظائف الرئيسية:**

1. **حساب تلقائي:**
   - حساب إجمالي كل بند عند تغيير الكمية أو السعر
   - حساب المجموع الفرعي للطلب
   - حساب الضريبة (15%)
   - حساب الإجمالي النهائي

2. **التحقق (Validation):**
   - المورد مطلوب
   - البنود مطلوبة (1 على الأقل)
   - الكميات > 0
   - الأسعار > 0
   - تاريخ التسليم > تاريخ الطلب

3. **نظام الحالات:**
   - Draft: يمكن تعديله وحذفه
   - Pending: في انتظار الاعتماد
   - Approved: معتمد، يمكن استلامه
   - PartiallyReceived: بدأ الاستلام
   - Received: مكتمل، لا يمكن تعديله
   - Cancelled: ملغي، لا يمكن تعديله

4. **الطباعة:**
   - تصميم طلب شراء احترافي
   - معلومات الشركة
   - معلومات المورد
   - جدول البنود
   - الحسابات
   - التوقيعات

**الوقت:** 12-14 ساعات (1.5 يوم)

---

### اليوم 5: PurchaseReceivingForm.cs (~700 سطر)

**الهيكل:**

```text
TabControl:
  1. استلام المشتريات (Receiving)
  2. سجل الاستلام (Receiving History)
```

*Tab 1: استلام المشتريات**

**القسم العلوي:**

- اختيار طلب الشراء (ComboBox - Approved Orders Only)
  - عرض: رقم الطلب، المورد، التاريخ، الحالة
- رقم المستند (TextBox - الفاتورة من المورد)
- تاريخ الاستلام (DateTimePicker)
- المستلم بواسطة (ComboBox - Employee)
- المخزن (ComboBox)

**قسم بنود الطلب (DataGridView):**

```text
Columns:
- ☑ استلام (CheckBox)
- البند (Label)
- الوحدة (Label)
- الكمية المطلوبة (Label)
- المستلم سابقاً (Label)
- المتبقي (Label)
- الكمية المستلمة (NumericUpDown)
- الحالة عند الاستلام (ComboBox: جيد، تالف، منتهي الصلاحية)
- ملاحظات (TextBox)
```

**إحصائيات سريعة:**

```text
إجمالي البنود: XX
المكتمل: XX
المتبقي: XX
نسبة الإنجاز: XX%
```

**قسم الملاحظات:**

- ملاحظات الاستلام (TextBox, multiline)
- الحالة العامة (ComboBox: جيد، بحاجة للفحص، مشاكل)

**الأزرار:**

- [استلام المحدد] - استلام البنود المحددة
- [استلام الكل] - استلام كل البنود بالكمية الكاملة
- [حفظ] - حفظ الاستلام وتحديث المخزون
- [إلغاء] - Cancel

*Tab 2: سجل الاستلام**

**الفلاتر:**

- طلب الشراء (ComboBox)
- المورد (ComboBox)
- من تاريخ (DateTimePicker)
- إلى تاريخ (DateTimePicker)
- بحث (TextBox)

**DataGridView:**

```text
Columns:
- رقم الطلب
- رقم المستند
- التاريخ
- المورد
- البند
- الكمية المستلمة
- الحالة
- المستلم بواسطة
```

**الأزرار:**

- [تحديث]
- [تصدير Excel]

**الوظائف الرئيسية:**

1. **تحديث المخزون:**
   - عند الحفظ، إنشاء StockMovement (نوع: Purchase)
   - تحديث CurrentStock في InventoryItem
   - ربط الحركة بـ PurchaseOrder

2. **تحديث حالة الطلب:**
   - حساب ReceivedQuantity لكل بند
   - إذا كل البنود مستلمة بالكامل: Status = Received
   - إذا بعض البنود مستلمة: Status = PartiallyReceived

3. **معالجة البنود التالفة:**
   - إذا الحالة = "تالف"، إنشاء حركة منفصلة للإتلاف
   - تسجيل في ملاحظات الحركة

4. **التحقق:**
   - الكمية المستلمة <= المتبقي
   - الكمية المستلمة > 0
   - رقم المستند مطلوب

**الوقت:** 10-12 ساعات (1.5 يوم)

---

### اليوم 6-7: PurchaseReportsForm.cs (~800 سطر)

**الهيكل:**

```text
TabControl:
  1. تقرير المشتريات (Purchases Report)
  2. تقرير الموردين (Suppliers Report)
  3. تحليل الإنفاق (Spending Analysis)
```

*Tab 1: تقرير المشتريات**

**الفلاتر:**

- من تاريخ (DateTimePicker)
- إلى تاريخ (DateTimePicker)
- المورد (ComboBox)
- الحالة (ComboBox)
- البند (ComboBox)

**DataGridView:**

```text
Columns:
- رقم الطلب
- التاريخ
- المورد
- البند
- الكمية
- سعر الوحدة
- الإجمالي
- الحالة
- المستلم
```

**الملخص (Panel):**

```text
إجمالي الطلبات: XXX
إجمالي البنود: XXX
إجمالي المشتريات: XXX,XXX.XX ريال
إجمالي الضريبة: XX,XXX.XX ريال
الإجمالي النهائي: XXX,XXX.XX ريال
```

*Tab 2: تقرير الموردين**

**الفلاتر:**

- من تاريخ (DateTimePicker)
- إلى تاريخ (DateTimePicker)
- المورد (ComboBox)

**DataGridView:**

```text
Columns:
- المورد
- عدد الطلبات
- إجمالي المشتريات
- الضريبة
- الإجمالي
- متوسط الطلب
- الحالة (Active/Inactive)
```

**رسم بياني:**

- Chart: أفضل 10 موردين حسب الإنفاق (Bar Chart)

*Tab 3: تحليل الإنفاق**

**الفلاتر:**

- من تاريخ (DateTimePicker)
- إلى تاريخ (DateTimePicker)
- نوع التحليل:
  - حسب المورد (By Supplier)
  - حسب الفئة (By Category)
  - حسب الشهر (By Month)
  - حسب البند (By Item)

**Charts:**

1. **Pie Chart:** توزيع الإنفاق
2. **Line Chart:** اتجاه الإنفاق عبر الزمن

**KPIs:**

```text
┌─────────────────────────────────────┐
│ إجمالي الإنفاق (الفترة)           │
│ XXX,XXX.XX ريال                    │
│ ↑ +15% عن الفترة السابقة          │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ متوسط قيمة الطلب                  │
│ XX,XXX.XX ريال                     │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ عدد الموردين النشطين              │
│ XX مورد                            │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ الطلبات المتأخرة                  │
│ X طلبات                            │
└─────────────────────────────────────┘
```

**الأزرار (لكل Tab):**

- [تحديث]
- [تصدير Excel]
- [تصدير PDF]
- [طباعة]

**الوقت:** 12-14 ساعات (1.5 يوم)

---

## 🔧 التكامل

### مع MainForm.cs

**إضافة أزرار:**

```csharp
// في قسم المشتريات (Purchasing Section)
Button btnPurchaseOrders = new Button { Text = "طلبات الشراء" };
Button btnReceiving = new Button { Text = "استلام المشتريات" };
Button btnPurchaseReports = new Button { Text = "تقارير المشتريات" };

btnPurchaseOrders.Click += (s, e) => new PurchaseOrderForm().ShowDialog();
btnReceiving.Click += (s, e) => new PurchaseReceivingForm().ShowDialog();
btnPurchaseReports.Click += (s, e) => new PurchaseReportsForm().ShowDialog();
```

### مع FishFarmContext.cs

```csharp
public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
```

---

## 📊 معايير الجودة

### SOCPA Compliance

- ✅ تتبع دقيق للمشتريات
- ✅ فصل واضح للصلاحيات (طلب، اعتماد، استلام)
- ✅ تسجيل كامل للمستندات
- ✅ حساب صحيح للضريبة 15%
- ✅ تكامل مع المخزون
- ✅ تقارير شاملة

### Testing Checklist

- [ ] إنشاء طلب شراء جديد
- [ ] حساب تلقائي للمبالغ والضريبة
- [ ] نظام الاعتماد
- [ ] استلام كامل
- [ ] استلام جزئي
- [ ] معالجة البنود التالفة
- [ ] تحديث المخزون
- [ ] التقارير والفلاتر
- [ ] التصدير (Excel/PDF)
- [ ] الطباعة

---

## 📅 الجدول الزمني التفصيلي

| اليوم | المهمة | الساعات | الحالة |
|-------|---------|---------|---------|
| **الإثنين** | PurchaseOrder.cs + PurchaseOrderItem.cs | 6 | ⏳ |
| **الإثنين** | Migration + Testing | 2 | ⏳ |
| **الثلاثاء** | PurchaseOrderForm.cs (Part 1) | 8 | ⏳ |
| **الأربعاء** | PurchaseOrderForm.cs (Part 2) | 6 | ⏳ |
| **الأربعاء** | Testing PurchaseOrderForm | 2 | ⏳ |
| **الخميس** | PurchaseReceivingForm.cs | 10 | ⏳ |
| **الجمعة** | PurchaseReceivingForm.cs | 2 | ⏳ |
| **الجمعة** | PurchaseReportsForm.cs (Part 1) | 6 | ⏳ |
| **السبت** | PurchaseReportsForm.cs (Part 2) | 8 | ⏳ |
| **الأحد** | Integration + Testing | 6 | ⏳ |
| **الأحد** | Documentation | 2 | ⏳ |

**الإجمالي:** 58 ساعة = 7 أيام عمل

---

## ✅ معايير الإنجاز

عند إنهاء الأسبوع 7، يجب تحقيق:

- ✅ 2 نماذج بيانات كاملة
- ✅ 3 نماذج واجهة كاملة
- ✅ ~2,800 سطر برمجي جديد
- ✅ Migration مطبقة
- ✅ تكامل مع المخزون
- ✅ تكامل مع الموردين
- ✅ تقارير شاملة
- ✅ 0 أخطاء في البناء
- ✅ اختبار شامل
- ✅ توثيق كامل

**النسبة المستهدفة:** 75% من المشروع ✅

---

## 🎯 الخطوة التالية

بعد إتمام الأسبوع 7:

- ✅ نظام المخزون (100%)
- ✅ نظام المشتريات (100%)
- ⏳ **الأسبوع 8:** نظام الحصاد المتقدم
- ⏳ **الأسبوع 9:** نظام الأصول الثابتة

**الموعد النهائي للمشروع:** 30 نوفمبر 2025

---

**تاريخ الإنشاء:** 6 أكتوبر 2025  
**المُعد:** GitHub Copilot  
**الحالة:** جاهز للتنفيذ
