using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج البركة - يحتوي على جميع المعلومات المطلوبة لإدارة البرك
    /// Pond Model - Contains all required information for pond management
    /// </summary>
    public class Pond
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم البركة")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "السعة (متر مكعب)")]
        public decimal Capacity { get; set; }
        
        [Required]
        [Display(Name = "المساحة (متر مربع)")]
        public decimal Area { get; set; }
        
        [Required]
        [Display(Name = "العمق (متر)")]
        public decimal Depth { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public PondStatus Status { get; set; }
        
        [Required]
        [Display(Name = "نوع البركة")]
        public PondType PondType { get; set; }
        
        [Display(Name = "نظام التهوية")]
        public AerationSystem AerationSystem { get; set; }
        
        [Display(Name = "نظام التصفية")]
        public FiltrationSystem FiltrationSystem { get; set; }
        
        [Display(Name = "نظام التسخين")]
        public HeatingSystem HeatingSystem { get; set; }
        
        [Display(Name = "نظام التبريد")]
        public CoolingSystem CoolingSystem { get; set; }
        
        [Display(Name = "سعة الأسماك")]
        public int FishCapacity { get; set; }
        
        [Display(Name = "درجة حرارة الماء المثلى")]
        public decimal? OptimalWaterTemperature { get; set; }
        
        [Display(Name = "درجة حموضة الماء المثلى")]
        public decimal? OptimalWaterPH { get; set; }
        
        [Display(Name = "مستوى الأكسجين المثلى")]
        public decimal? OptimalOxygenLevel { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Display(Name = "تاريخ الإنشاء")]
        public DateTime? CreatedDate { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<ProductionCyclePond> ProductionCyclePonds { get; set; } = new List<ProductionCyclePond>();
        public virtual ICollection<WaterQualityRecord> WaterQualityRecords { get; set; } = new List<WaterQualityRecord>();
        public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
        
        // Helper methods
        public decimal CalculateVolume()
        {
            return Area * Depth;
        }
        
        public decimal CalculateFishDensity(int fishCount)
        {
            return (decimal)fishCount / Capacity;
        }
    }
}