// ========================================
// النماذج المقترحة لإكمال النظام المحاسبي
// Fish Farm Manager - Proposed Models
// ========================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models.Proposed
{
    // ============================================
    // 1. نظام إدارة العملاء والمبيعات
    // ============================================

    /// <summary>
    /// نموذج بيانات العملاء - يجب إضافته
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public CustomerType Type { get; set; }
        
        [Phone]
        public string Phone { get; set; } = string.Empty;
        
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        [StringLength(15)]
        public string TaxNumber { get; set; } = string.Empty; // الرقم الضريبي
        
        [StringLength(10)]
        public string CommercialRegistration { get; set; } = string.Empty; // السجل التجاري
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; } = 0; // حد الائتمان
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0; // الرصيد الحالي
        
        public int PaymentTermDays { get; set; } = 30; // مدة السداد بالأيام
        
        public CustomerStatus Status { get; set; } = CustomerStatus.Active;
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        
        // العلاقات
        public List<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
        public List<CustomerPayment> Payments { get; set; } = new List<CustomerPayment>();
    }

    public enum CustomerType
    {
        Retail = 1,        // قطاعي
        Wholesale = 2,     // جملة
        Restaurant = 3,    // مطعم
        Hotel = 4,         // فندق
        Supermarket = 5,   // سوبر ماركت
        Exporter = 6,      // مصدر
        Other = 7          // أخرى
    }

    public enum CustomerStatus
    {
        Active = 1,
        Suspended = 2,
        Inactive = 3
    }

    /// <summary>
    /// نموذج أوامر المبيعات - يجب إضافته
    /// </summary>
    public class SalesOrder
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = string.Empty; // رقم الطلب
        
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        
        [Required]
        public DateTime OrderDate { get; set; }
        
        public DateTime? DeliveryDate { get; set; }
        
        public DateTime? ExpectedDeliveryDate { get; set; }
        
        public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Pending;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal VATAmount { get; set; } // ضريبة القيمة المضافة
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }
        
        public string DeliveryAddress { get; set; } = string.Empty;
        public string DeliveryInstructions { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        
        // العلاقات
        public List<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
        public List<CustomerPayment> Payments { get; set; } = new List<CustomerPayment>();
    }

    public enum SalesOrderStatus
    {
        Pending = 1,       // معلق
        Confirmed = 2,     // مؤكد
        InProgress = 3,    // قيد التنفيذ
        ReadyToDeliver = 4,// جاهز للتسليم
        Delivered = 5,     // تم التسليم
        Cancelled = 6,     // ملغى
        Returned = 7       // مرتجع
    }

    /// <summary>
    /// نموذج بنود أوامر المبيعات - يجب إضافته
    /// </summary>
    public class SalesOrderItem
    {
        public int Id { get; set; }
        
        public int SalesOrderId { get; set; }
        public SalesOrder SalesOrder { get; set; } = null!;
        
        public int? ProductionCycleId { get; set; } // ربط بدورة الإنتاج
        public ProductionCycle? ProductionCycle { get; set; }
        
        public int? HarvestRecordId { get; set; } // ربط بسجل الحصاد
        
        [Required]
        public string ProductName { get; set; } = string.Empty; // اسم المنتج (نوع السمك)
        
        public FishGrade Grade { get; set; }
        
        [Required]
        public double Quantity { get; set; } // الكمية بالكيلوجرام
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // سعر الكيلو
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        public string Notes { get; set; } = string.Empty;
    }

    public enum FishGrade
    {
        Premium = 1,   // ممتاز
        Grade_A = 2,   // درجة أولى
        Grade_B = 3,   // درجة ثانية
        Grade_C = 4,   // درجة ثالثة
        Rejected = 5   // مرفوض
    }

    /// <summary>
    /// نموذج مدفوعات العملاء - يجب إضافته
    /// </summary>
    public class CustomerPayment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string PaymentNumber { get; set; } = string.Empty;
        
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        
        public int? SalesOrderId { get; set; }
        public SalesOrder? SalesOrder { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; }
        
        public string ReferenceNumber { get; set; } = string.Empty; // رقم الشيك/الحوالة
        public string BankName { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum PaymentMethod
    {
        Cash = 1,           // نقدي
        Check = 2,          // شيك
        BankTransfer = 3,   // حوالة بنكية
        CreditCard = 4,     // بطاقة ائتمان
        DebitCard = 5,      // بطاقة مدى
        Other = 6           // أخرى
    }

    // ============================================
    // 2. نظام إدارة الموردين والمشتريات
    // ============================================

    /// <summary>
    /// نموذج بيانات الموردين - يجب إضافته
    /// </summary>
    public class Supplier
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public SupplierType Type { get; set; }
        
        [Phone]
        public string Phone { get; set; } = string.Empty;
        
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        [StringLength(15)]
        public string TaxNumber { get; set; } = string.Empty;
        
        public string ContactPerson { get; set; } = string.Empty;
        
        public int PaymentTermDays { get; set; } = 30;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0; // المبلغ المستحق
        
        public SupplierStatus Status { get; set; } = SupplierStatus.Active;
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        
        // العلاقات
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public List<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
    }

    public enum SupplierType
    {
        Fingerlings = 1,    // زريعة
        Feed = 2,           // أعلاف
        Medicine = 3,       // أدوية
        Equipment = 4,      // معدات
        Chemicals = 5,      // مواد كيميائية
        SpareParts = 6,     // قطع غيار
        Services = 7,       // خدمات
        Other = 8           // أخرى
    }

    public enum SupplierStatus
    {
        Active = 1,
        Suspended = 2,
        Inactive = 3
    }

    /// <summary>
    /// نموذج أوامر الشراء - يجب إضافته
    /// </summary>
    public class PurchaseOrder
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = string.Empty;
        
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        
        [Required]
        public DateTime OrderDate { get; set; }
        
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal VATAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }
        
        public string DeliveryLocation { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        
        // العلاقات
        public List<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public List<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
    }

    public enum PurchaseOrderStatus
    {
        Pending = 1,       // معلق
        Approved = 2,      // موافق عليه
        Ordered = 3,       // تم الطلب
        PartiallyReceived = 4, // استلام جزئي
        Received = 5,      // تم الاستلام
        Cancelled = 6      // ملغى
    }

    /// <summary>
    /// نموذج بنود أوامر الشراء - يجب إضافته
    /// </summary>
    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        
        public int? InventoryItemId { get; set; } // ربط بالمخزون
        
        [Required]
        public string ItemName { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public double Quantity { get; set; }
        
        public double ReceivedQuantity { get; set; } = 0;
        
        [Required]
        public string Unit { get; set; } = string.Empty; // كجم، لتر، قطعة، إلخ
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// نموذج مدفوعات الموردين - يجب إضافته
    /// </summary>
    public class SupplierPayment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string PaymentNumber { get; set; } = string.Empty;
        
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        
        public int? PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; }
        
        public string ReferenceNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        
        public string Notes { get; set; } = string.Empty;
        public string PaidBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // ============================================
    // 3. نظام إدارة المخزون
    // ============================================

    /// <summary>
    /// نموذج بنود المخزون - يجب إضافته
    /// </summary>
    public class InventoryItem
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ItemCode { get; set; } = string.Empty; // كود الصنف
        
        [Required]
        public string ItemName { get; set; } = string.Empty;
        
        public InventoryCategory Category { get; set; }
        
        public string Brand { get; set; } = string.Empty; // العلامة التجارية
        public string Manufacturer { get; set; } = string.Empty; // الشركة المصنعة
        
        [Required]
        public string Unit { get; set; } = string.Empty; // كجم، لتر، قطعة
        
        public double CurrentQuantity { get; set; } = 0;
        public double MinimumQuantity { get; set; } = 0; // حد إعادة الطلب
        public double MaximumQuantity { get; set; } = 0; // الحد الأقصى
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; } = 0; // تكلفة الوحدة
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; } = 0; // القيمة الإجمالية
        
        public string StorageLocation { get; set; } = string.Empty; // موقع التخزين
        
        public DateTime? ExpiryDate { get; set; } // تاريخ الصلاحية
        public DateTime? ManufactureDate { get; set; }
        
        public string BatchNumber { get; set; } = string.Empty; // رقم الدفعة
        
        public InventoryItemStatus Status { get; set; } = InventoryItemStatus.Available;
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        
        // العلاقات
        public List<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }

    public enum InventoryCategory
    {
        Feed_Starter = 1,       // علف بادئ
        Feed_Grower = 2,        // علف نمو
        Feed_Finisher = 3,      // علف تسمين
        Medicine_Antibiotic = 4,// مضاد حيوي
        Medicine_Vaccine = 5,   // لقاح
        Chemical_Disinfectant = 6, // مطهر
        Chemical_Fertilizer = 7,// سماد
        Equipment_Aerator = 8,  // مؤكسج
        Equipment_Net = 9,      // شبكة
        SparePart = 10,         // قطعة غيار
        Packaging = 11,         // تغليف
        Fingerlings = 12,       // زريعة
        Other = 13             // أخرى
    }

    public enum InventoryItemStatus
    {
        Available = 1,      // متوفر
        LowStock = 2,       // مخزون منخفض
        OutOfStock = 3,     // غير متوفر
        Expired = 4,        // منتهي الصلاحية
        Damaged = 5         // تالف
    }

    /// <summary>
    /// نموذج حركات المخزون - يجب إضافته
    /// </summary>
    public class StockMovement
    {
        public int Id { get; set; }
        
        public int InventoryItemId { get; set; }
        public InventoryItem InventoryItem { get; set; } = null!;
        
        [Required]
        public DateTime MovementDate { get; set; }
        
        public StockMovementType MovementType { get; set; }
        
        public double Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }
        
        public double BalanceAfter { get; set; } // الرصيد بعد الحركة
        
        public int? PurchaseOrderId { get; set; }
        public int? ProductionCycleId { get; set; }
        
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum StockMovementType
    {
        PurchaseIn = 1,         // وارد شراء
        SalesOut = 2,           // صادر بيع
        ProductionUse = 3,      // استخدام إنتاج
        Adjustment_Increase = 4,// تسوية زيادة
        Adjustment_Decrease = 5,// تسوية نقص
        Transfer_In = 6,        // نقل وارد
        Transfer_Out = 7,       // نقل صادر
        Return_In = 8,          // مرتجع وارد
        Return_Out = 9,         // مرتجع صادر
        Damaged = 10,           // تالف
        Expired = 11            // منتهي الصلاحية
    }

    // ============================================
    // 4. نظام التكاليف الشاملة
    // ============================================

    /// <summary>
    /// نموذج سجلات التكاليف - تطوير الموجود
    /// </summary>
    public class CostRecord
    {
        public int Id { get; set; }
        
        public int? ProductionCycleId { get; set; }
        public ProductionCycle? ProductionCycle { get; set; }
        
        public int? PondId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public CostCategory Category { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public double? Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? PaymentDueDate { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum CostCategory
    {
        // تكاليف مباشرة
        Fingerlings = 1,            // زريعة
        Feed = 2,                   // أعلاف
        Medicine_Treatment = 3,     // أدوية وعلاجات
        Labor_Production = 4,       // عمالة إنتاج
        
        // تكاليف غير مباشرة
        Electricity = 5,            // كهرباء
        Water = 6,                  // مياه
        Fuel = 7,                   // وقود
        
        // تكاليف صيانة
        Maintenance_Pond = 8,       // صيانة أحواض
        Maintenance_Equipment = 9,  // صيانة معدات
        
        // تكاليف إدارية
        Labor_Administrative = 10,  // عمالة إدارية
        Rent = 11,                  // إيجار
        Insurance = 12,             // تأمين
        Licenses_Fees = 13,         // رخص ورسوم
        
        // تكاليف تسويق وبيع
        Transportation = 14,        // نقل
        Packaging = 15,             // تغليف
        Marketing = 16,             // تسويق
        Sales_Commission = 17,      // عمولة مبيعات
        
        // تكاليف أخرى
        Chemicals = 18,             // مواد كيميائية
        Testing_Analysis = 19,      // فحوصات وتحاليل
        Certification = 20,         // شهادات
        Depreciation = 21,          // إهلاك
        Finance_Charges = 22,       // رسوم تمويل
        Other = 23                  // أخرى
    }

    public enum PaymentStatus
    {
        Unpaid = 1,         // غير مدفوع
        PartiallyPaid = 2,  // مدفوع جزئياً
        Paid = 3,           // مدفوع بالكامل
        Overdue = 4         // متأخر
    }

    // ============================================
    // 5. نظام الحصاد المتقدم
    // ============================================

    /// <summary>
    /// نموذج سجلات الحصاد المفصلة - يجب إضافته
    /// </summary>
    public class HarvestRecord
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string HarvestNumber { get; set; } = string.Empty;
        
        public int ProductionCycleId { get; set; }
        public ProductionCycle ProductionCycle { get; set; } = null!;
        
        [Required]
        public DateTime HarvestDate { get; set; }
        
        public HarvestType HarvestType { get; set; }
        
        public double TotalWeight { get; set; } // كجم
        public int FishCount { get; set; }
        public double AverageWeight { get; set; } // جرام
        
        public string HarvestMethod { get; set; } = string.Empty; // شبكة، صيد، إلخ
        
        public string WeatherConditions { get; set; } = string.Empty;
        public double? WaterTemperature { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedMarketValue { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // العلاقات
        public List<HarvestGrade> Grades { get; set; } = new List<HarvestGrade>();
    }

    public enum HarvestType
    {
        Final = 1,          // حصاد نهائي
        Partial = 2,        // حصاد جزئي
        Sampling = 3,       // عينة
        Emergency = 4       // طارئ
    }

    /// <summary>
    /// نموذج تصنيف الحصاد حسب الجودة - يجب إضافته
    /// </summary>
    public class HarvestGrade
    {
        public int Id { get; set; }
        
        public int HarvestRecordId { get; set; }
        public HarvestRecord HarvestRecord { get; set; } = null!;
        
        public FishGrade Grade { get; set; }
        
        public double Weight { get; set; } // كجم
        public int Count { get; set; }
        public double AverageWeight { get; set; } // جرام
        
        public double WeightPercentage { get; set; } // نسبة الوزن من الإجمالي
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerKg { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; }
        
        public string Notes { get; set; } = string.Empty;
    }

    // ============================================
    // 6. نظام الأصول الثابتة
    // ============================================

    /// <summary>
    /// نموذج الأصول الثابتة - يجب إضافته
    /// </summary>
    public class FixedAsset
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string AssetCode { get; set; } = string.Empty;
        
        [Required]
        public string AssetName { get; set; } = string.Empty;
        
        public AssetCategory Category { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime PurchaseDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; }
        
        public int UsefulLifeYears { get; set; } // العمر الافتراضي
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AnnualDepreciation { get; set; } // الإهلاك السنوي
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AccumulatedDepreciation { get; set; } // الإهلاك المتراكم
        
        public AssetStatus Status { get; set; } = AssetStatus.Active;
        
        public string Location { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public enum AssetCategory
    {
        Land = 1,               // أراضي
        Building = 2,           // مباني
        Pond = 3,               // أحواض
        Aerator = 4,            // مؤكسجات
        Generator = 5,          // مولدات
        Pump = 6,               // مضخات
        Vehicle = 7,            // مركبات
        Equipment = 8,          // معدات
        Furniture = 9,          // أثاث
        ComputerSystem = 10,    // أنظمة حاسوبية
        Other = 11              // أخرى
    }

    public enum AssetStatus
    {
        Active = 1,         // نشط
        Maintenance = 2,    // صيانة
        Retired = 3,        // متقاعد
        Sold = 4,           // مباع
        Damaged = 5         // تالف
    }

    // ============================================
    // 7. نظام التتبع (Traceability)
    // ============================================

    /// <summary>
    /// نموذج التتبع من المزرعة للمستهلك - يجب إضافته
    /// </summary>
    public class TraceabilityRecord
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TraceCode { get; set; } = string.Empty; // رمز التتبع الفريد (QR Code)
        
        public int ProductionCycleId { get; set; }
        public ProductionCycle ProductionCycle { get; set; } = null!;
        
        public int? HarvestRecordId { get; set; }
        public HarvestRecord? HarvestRecord { get; set; }
        
        public int? SalesOrderId { get; set; }
        public SalesOrder? SalesOrder { get; set; }
        
        // معلومات المصدر
        public string FingerlingsSource { get; set; } = string.Empty;
        public DateTime StockingDate { get; set; }
        public string Breed { get; set; } = string.Empty; // السلالة
        public string GeneticLineage { get; set; } = string.Empty; // السلالة الوراثية
        
        // معلومات الإنتاج
        public string FarmName { get; set; } = string.Empty;
        public string FarmLocation { get; set; } = string.Empty;
        public string PondNumber { get; set; } = string.Empty;
        
        // معلومات الأعلاف
        public string FeedBrand { get; set; } = string.Empty;
        public string FeedType { get; set; } = string.Empty;
        
        // معلومات العلاجات
        public bool AntibioticsUsed { get; set; } = false;
        public string TreatmentDetails { get; set; } = string.Empty;
        public int WithdrawalPeriodDays { get; set; } = 0;
        
        // معلومات الحصاد
        public DateTime? HarvestDate { get; set; }
        public string HarvestMethod { get; set; } = string.Empty;
        
        // معلومات الجودة
        public bool QualityTestPassed { get; set; } = false;
        public string TestingLaboratory { get; set; } = string.Empty;
        public string CertificateNumber { get; set; } = string.Empty;
        
        // معلومات البيع
        public DateTime? SaleDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        
        // سلسلة الحفظ
        public string ChainOfCustody { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ============================================
    // 8. نظام التخطيط والموازنة
    // ============================================

    /// <summary>
    /// نموذج خطة الإنتاج - يجب إضافته
    /// </summary>
    public class ProductionPlan
    {
        public int Id { get; set; }
        
        [Required]
        public string PlanName { get; set; } = string.Empty;
        
        public int Year { get; set; }
        public int Quarter { get; set; }
        
        public int TargetCycles { get; set; }
        public double TargetProduction { get; set; } // كجم
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetRevenue { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetedCosts { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetProfit { get; set; }
        
        public PlanStatus Status { get; set; } = PlanStatus.Draft;
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        
        // العلاقات
        public List<PlannedCycle> PlannedCycles { get; set; } = new List<PlannedCycle>();
    }

    public enum PlanStatus
    {
        Draft = 1,          // مسودة
        Approved = 2,       // موافق عليها
        InProgress = 3,     // قيد التنفيذ
        Completed = 4,      // مكتملة
        Cancelled = 5       // ملغاة
    }

    /// <summary>
    /// نموذج الدورات المخططة - يجب إضافته
    /// </summary>
    public class PlannedCycle
    {
        public int Id { get; set; }
        
        public int ProductionPlanId { get; set; }
        public ProductionPlan ProductionPlan { get; set; } = null!;
        
        public string CycleName { get; set; } = string.Empty;
        
        public DateTime PlannedStartDate { get; set; }
        public DateTime PlannedEndDate { get; set; }
        
        public int PondId { get; set; }
        
        public int PlannedFingerlingsCount { get; set; }
        public double PlannedFinalWeight { get; set; }
        public double PlannedProduction { get; set; } // كجم
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetedCosts { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedRevenue { get; set; }
        
        public int? ActualProductionCycleId { get; set; } // ربط بالدورة الفعلية
        
        public string Notes { get; set; } = string.Empty;
    }

    // ============================================
    // 9. إضافات على نموذج ProductionCycle الموجود
    // ============================================

    /// <summary>
    /// ملحق لنموذج ProductionCycle - خصائص إضافية مقترحة
    /// </summary>
    public class ProductionCycleExtended
    {
        // يجب إضافة هذه الحقول إلى ProductionCycle الموجود
        
        // البيانات المالية
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal ROI { get; set; } // العائد على الاستثمار
        
        // بيانات السلالة
        public string BreedName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public WaterType WaterType { get; set; }
        
        // بيانات التكاليف
        public decimal FingerlingsoCost { get; set; }
        public decimal FeedCost { get; set; }
        public decimal LaborCost { get; set; }
        public decimal UtilitiesCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal OtherCosts { get; set; }
        
        // بيانات الكفاءة
        public double FeedEfficiency { get; set; }
        public double ProductionEfficiency { get; set; }
        public double CostPerKg { get; set; }
    }

    public enum WaterType
    {
        Freshwater = 1,     // مياه عذبة (< 0.5 ppt)
        Brackish = 2,       // مياه شروب (0.5-30 ppt)
        Marine = 3          // مياه مالحة (> 30 ppt)
    }
}
