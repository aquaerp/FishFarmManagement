using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج المعدات - يحتوي على جميع المعلومات المطلوبة لإدارة المعدات
    /// Equipment Model - Contains all required information for equipment management
    /// </summary>
    public class Equipment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم المعدة")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم المعدة")]
        public string EquipmentNumber { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "الفئة")]
        public string Category { get; set; } = string.Empty;
        
        [StringLength(100)]
        [Display(Name = "الرقم التسلسلي")]
        public string? SerialNumber { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الموديل")]
        public string? Model { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الشركة المصنعة")]
        public string? Manufacturer { get; set; }
        
        [Display(Name = "سعر الشراء")]
        public decimal PurchasePrice { get; set; }
        
        [StringLength(100)]
        [Display(Name = "المورد")]
        public string? Supplier { get; set; }
        
        [Display(Name = "فترة الضمان (أشهر)")]
        public int WarrantyPeriod { get; set; }
        
        [Display(Name = "تاريخ انتهاء الضمان")]
        public DateTime? WarrantyExpiryDate { get; set; }
        
        [Display(Name = "انتهى الضمان")]
        public DateTime? WarrantyExpiry { get; set; }
        
        [Display(Name = "تاريخ التركيب")]
        public DateTime? InstallationDate { get; set; }
        
        [Display(Name = "البركة")]
        public int? PondId { get; set; }
        
        [StringLength(200)]
        [Display(Name = "الموقع")]
        public string? Location { get; set; }
        
        [Display(Name = "ساعات التشغيل")]
        public decimal OperatingHours { get; set; }
        
        [Display(Name = "تاريخ آخر صيانة")]
        public DateTime? LastMaintenanceDate { get; set; }
        
        [Display(Name = "تاريخ الصيانة القادمة")]
        public DateTime? NextMaintenanceDate { get; set; }
        
        [Display(Name = "فترة الصيانة (أيام)")]
        public int MaintenanceIntervalDays { get; set; }
        
        [Display(Name = "تكلفة الصيانة السنوية")]
        public decimal AnnualMaintenanceCost { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "المواصفات")]
        public string? Specifications { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات الأمان")]
        public string? SafetyNotes { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Display(Name = "الحالة")]
        public string? Status { get; set; }
        
        [Display(Name = "تاريخ الشراء")]
        public DateTime? PurchaseDate { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        [Display(Name = "تاريخ التعديل")]
        public DateTime? ModifiedDate { get; set; }
        
        // Navigation properties
        public virtual Pond? Pond { get; set; }
        public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
    }
}