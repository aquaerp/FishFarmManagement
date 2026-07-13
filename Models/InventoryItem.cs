using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    public enum InventoryStatus
    {
        Active,            // نشط
        Inactive,          // غير نشط
        Discontinued,      // متوقف
        LowStock,          // مخزون منخفض
        OutOfStock         // نفد المخزون
    }

    /// <summary>
    /// نموذج بيانات عنصر المخزون
    /// </summary>
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? ItemName { get; set; }

        [StringLength(100)]
        public string? Code { get; set; }
        
        [StringLength(100)]
        public string? ItemCode { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public InventoryCategory Category { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "كجم";
        
        [StringLength(50)]
        public string? UnitOfMeasure { get; set; }
        
        public decimal? RequiredTemperature { get; set; }
        
        public decimal? RequiredHumidity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentStock { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal MinimumStock { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal MaximumStock { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? ReorderPoint { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? ReorderQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LastPurchasePrice { get; set; }

        public DateTime? LastPurchaseDate { get; set; }

        [StringLength(100)]
        public string? StorageLocation { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }

        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastModifiedDate { get; set; }
        
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
        
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        
        // خصائص إضافية مطلوبة من FishFarmContext
        public InventoryStatus Status { get; set; } = InventoryStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<InventoryValuation> InventoryValuations { get; set; } = new List<InventoryValuation>();
        
        // Computed Properties
        [NotMapped]
        public decimal SellingPrice 
        { 
            get => UnitCost * 1.2m; // 20% markup
            set { } // Setter فارغ للتوافق مع الاستخدامات القديمة
        }
        
        [NotMapped]
        public decimal StockValue => CurrentStock * UnitCost;
        
        [NotMapped]
        public bool NeedsReorder => CurrentStock <= (ReorderPoint ?? MinimumStock);
        
        [NotMapped]
        public bool IsPerishable 
        { 
            get => ExpiryDate.HasValue;
            set { } // Setter فارغ للتوافق مع الاستخدامات القديمة
        }
        
        [NotMapped]
        public bool HasExpiryDate 
        { 
            get => ExpiryDate.HasValue;
            set { } // Setter فارغ للتوافق مع الاستخدامات القديمة
        }
        
        [NotMapped]
        public bool RequiresRefrigeration 
        { 
            get => Category == InventoryCategory.Medicine || Category == InventoryCategory.Vitamins;
            set { } // Setter فارغ للتوافق مع الاستخدامات القديمة
        }
        
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
        
        [NotMapped]
        public bool IsNearExpiry => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Now.AddDays(30);
        
        [NotMapped]
        public int? DaysToExpiry => ExpiryDate.HasValue ? (int?)(ExpiryDate.Value - DateTime.Now).TotalDays : null;
        
        public string GetStockStatusDisplay()
        {
            if (CurrentStock <= 0) return "نفد المخزون";
            if (CurrentStock <= MinimumStock) return "مخزون منخفض";
            if (CurrentStock >= MaximumStock) return "مخزون زائد";
            return "طبيعي";
        }
    }

    /// <summary>
    /// تصنيفات المخزون
    /// </summary>
    public enum InventoryCategory
    {
        Feed = 1,                  // أعلاف
        FishFeed = 2,              // أعلاف أسماك
        Fingerlings = 3,           // زريعة
        Medicine = 4,              // أدوية
        Medications = 5,           // أدوية (بديل)
        Vitamins = 6,              // فيتامينات
        Equipment = 7,             // معدات
        SafetyEquipment = 8,       // معدات سلامة
        SpareParts = 9,            // قطع غيار
        Chemicals = 10,            // مواد كيميائية
        Oxygen = 11,               // أكسجين
        Packaging = 12,            // تعبئة وتغليف
        PackagingMaterials = 13,   // مواد تعبئة وتغليف
        Fuel = 14,                 // وقود
        OfficeSupplies = 15,       // مستلزمات مكتبية
        CleaningSupplies = 16,     // مستلزمات تنظيف
        LaboratorySupplies = 17,   // مستلزمات مختبر
        FreshFish = 18,            // أسماك طازجة
        FrozenFish = 19,           // أسماك مجمدة
        Other = 98,                // أخرى
        Others = 99                // أخرى (بديل)
    }
}
