using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج الأصول الثابتة
    /// Fixed Asset Model
    /// </summary>
    public class FixedAsset
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الأصل")]
        public string AssetNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم الأصل")]
        public string AssetName { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "فئة الأصل")]
        public AssetCategory Category { get; set; }
        
        [Required]
        [Display(Name = "تاريخ الشراء")]
        public DateTime PurchaseDate { get; set; }
        
        [Required]
        [Display(Name = "تكلفة الشراء")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseCost { get; set; }
        
        [Display(Name = "القيمة المتبقية")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ResidualValue { get; set; }
        
        [Required]
        [Display(Name = "العمر الافتراضي (سنوات)")]
        public int UsefulLifeYears { get; set; }
        
        [Required]
        [Display(Name = "طريقة الإهلاك")]
        public DepreciationMethod DepreciationMethod { get; set; }
        
        [Display(Name = "نسبة الإهلاك السنوية")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal AnnualDepreciationRate { get; set; }
        
        [Display(Name = "الإهلاك المتراكم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AccumulatedDepreciation { get; set; }
        
        [Display(Name = "القيمة الدفترية")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BookValue { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الموقع")]
        public string? Location { get; set; }
        
        [StringLength(100)]
        [Display(Name = "القسم")]
        public string? Department { get; set; }
        
        [Display(Name = "الموظف المسؤول")]
        public int? ResponsibleEmployeeId { get; set; }
        
        [StringLength(50)]
        [Display(Name = "الرقم التسلسلي")]
        public string? SerialNumber { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم الفاتورة")]
        public string? InvoiceNumber { get; set; }
        
        [Display(Name = "تاريخ الفاتورة")]
        public DateTime? InvoiceDate { get; set; }
        
        [Display(Name = "المورد")]
        public int? SupplierId { get; set; }
        
        [StringLength(200)]
        [Display(Name = "الشركة المصنعة")]
        public string? Manufacturer { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الموديل")]
        public string? Model { get; set; }
        
        [Display(Name = "سنة التصنيع")]
        public int? ManufactureYear { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public AssetStatus Status { get; set; } = AssetStatus.Active;
        
        [Display(Name = "تاريخ البدء في الاستخدام")]
        public DateTime? InServiceDate { get; set; }
        
        [Display(Name = "تاريخ التوقف عن الاستخدام")]
        public DateTime? OutOfServiceDate { get; set; }
        
        [Display(Name = "تاريخ البيع/التخلص")]
        public DateTime? DisposalDate { get; set; }
        
        [Display(Name = "قيمة البيع/التخلص")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DisposalValue { get; set; }
        
        [Display(Name = "فترة الضمان (شهور)")]
        public int? WarrantyMonths { get; set; }
        
        [Display(Name = "تاريخ انتهاء الضمان")]
        public DateTime? WarrantyExpiryDate { get; set; }
        
        [Display(Name = "يتطلب تأمين")]
        public bool RequiresInsurance { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم بوليصة التأمين")]
        public string? InsurancePolicyNumber { get; set; }
        
        [Display(Name = "تاريخ انتهاء التأمين")]
        public DateTime? InsuranceExpiryDate { get; set; }
        
        [Display(Name = "يحتاج صيانة دورية")]
        public bool RequiresMaintenance { get; set; }
        
        [Display(Name = "تكرار الصيانة (أشهر)")]
        public int? MaintenanceFrequencyMonths { get; set; }
        
        [Display(Name = "تاريخ آخر صيانة")]
        public DateTime? LastMaintenanceDate { get; set; }
        
        [Display(Name = "تاريخ الصيانة القادمة")]
        public DateTime? NextMaintenanceDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        [Display(Name = "حُدّث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation Properties
        [ForeignKey(nameof(ResponsibleEmployeeId))]
        public virtual Employee? ResponsibleEmployee { get; set; }
        
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }
        
        public virtual ICollection<AssetDepreciation> Depreciations { get; set; } = new List<AssetDepreciation>();
        public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
        
        // Computed Properties
        [NotMapped]
        public int AgeInYears => (DateTime.Now - PurchaseDate).Days / 365;
        
        [NotMapped]
        public decimal CurrentBookValue => PurchaseCost - AccumulatedDepreciation;
        
        [NotMapped]
        public bool IsFullyDepreciated => AccumulatedDepreciation >= (PurchaseCost - ResidualValue);
        
        [NotMapped]
        public bool IsUnderWarranty => WarrantyExpiryDate.HasValue && WarrantyExpiryDate.Value > DateTime.Now;
        
        [NotMapped]
        public bool IsInsuranceExpired => InsuranceExpiryDate.HasValue && InsuranceExpiryDate.Value < DateTime.Now;
        
        [NotMapped]
        public bool MaintenanceDue => NextMaintenanceDate.HasValue && NextMaintenanceDate.Value <= DateTime.Now;
        
        // Helper Methods
        public decimal CalculateAnnualDepreciation()
        {
            var depreciableAmount = PurchaseCost - ResidualValue;
            
            return DepreciationMethod switch
            {
                DepreciationMethod.StraightLine => depreciableAmount / UsefulLifeYears,
                DepreciationMethod.DecliningBalance => CurrentBookValue * (AnnualDepreciationRate / 100),
                _ => 0
            };
        }
    }
    
    /// <summary>
    /// فئات الأصول الثابتة
    /// Asset Categories
    /// </summary>
    public enum AssetCategory
    {
        [Display(Name = "مباني وإنشاءات")]
        Buildings = 1,
        
        [Display(Name = "أحواض")]
        Ponds = 2,
        
        [Display(Name = "معدات إنتاج")]
        ProductionEquipment = 3,
        
        [Display(Name = "معدات تهوية")]
        AerationEquipment = 4,
        
        [Display(Name = "معدات تصفية")]
        FiltrationEquipment = 5,
        
        [Display(Name = "مولدات كهرباء")]
        Generators = 6,
        
        [Display(Name = "مضخات")]
        Pumps = 7,
        
        [Display(Name = "سيارات ومركبات")]
        Vehicles = 8,
        
        [Display(Name = "أجهزة كمبيوتر")]
        Computers = 9,
        
        [Display(Name = "أثاث")]
        Furniture = 10,
        
        [Display(Name = "معدات مكتبية")]
        OfficeEquipment = 11,
        
        [Display(Name = "معدات مخبرية")]
        LaboratoryEquipment = 12,
        
        [Display(Name = "أخرى")]
        Other = 99
    }
    
    /// <summary>
    /// حالة الأصل
    /// Asset Status
    /// </summary>
    public enum AssetStatus
    {
        [Display(Name = "نشط")]
        Active = 1,
        
        [Display(Name = "قيد الصيانة")]
        UnderMaintenance = 2,
        
        [Display(Name = "متوقف")]
        Inactive = 3,
        
        [Display(Name = "مُباع")]
        Sold = 4,
        
        [Display(Name = "مُستبعد")]
        Disposed = 5,
        
        [Display(Name = "مفقود")]
        Lost = 6,
        
        [Display(Name = "تالف")]
        Damaged = 7
    }
    
    /// <summary>
    /// طريقة الإهلاك
    /// Depreciation Method
    /// </summary>
    public enum DepreciationMethod
    {
        [Display(Name = "القسط الثابت")]
        StraightLine = 1,
        
        [Display(Name = "القسط المتناقص")]
        DecliningBalance = 2,
        
        [Display(Name = "مجموع أرقام السنوات")]
        SumOfYearsDigits = 3,
        
        [Display(Name = "وحدات الإنتاج")]
        UnitsOfProduction = 4
    }
}


