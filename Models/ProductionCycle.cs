using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج دورة الإنتاج - يحتوي على جميع المعلومات المطلوبة لإدارة دورات الإنتاج
    /// Production Cycle Model - Contains all required information for production cycle management
    /// </summary>
    public class ProductionCycle
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم دورة الإنتاج")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "تاريخ البداية")]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "تاريخ النهاية")]
        public DateTime? EndDate { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public CycleStatus Status { get; set; }
        
        [Required]
        [Display(Name = "نوع الدورة")]
        public CycleType CycleType { get; set; }
        
        [Required]
        [Display(Name = "عدد الأسماك الأولي")]
        public int InitialFishCount { get; set; }
        
        [Display(Name = "متوسط الوزن الأولي")]
        public decimal InitialAverageWeight { get; set; }
        
        [Display(Name = "مصدر الزريعة")]
        public string? FrySource { get; set; }
        
        [Display(Name = "مصدر المفرخ")]
        public string? HatcherySource { get; set; }
        
        [Display(Name = "تاريخ التفريخ المتوقع")]
        public DateTime? ExpectedHatchDate { get; set; }
        
        [Display(Name = "عدد الأسماك النهائي")]
        public int? FinalFishCount { get; set; }
        
        [Display(Name = "متوسط الوزن النهائي")]
        public decimal? FinalAverageWeight { get; set; }
        
        [Display(Name = "إجمالي وزن الحصاد")]
        public decimal? TotalHarvestWeight { get; set; }
        
        [Display(Name = "معدل البقاء")]
        public decimal? SurvivalRate { get; set; }
        
        [Display(Name = "معدل التحويل الغذائي")]
        public decimal? FCR { get; set; }
        
        [Display(Name = "متوسط النمو اليومي")]
        public decimal? ADG { get; set; }
        
        [Display(Name = "عدد اليرقات الفعلي")]
        public int? ActualLarvalCount { get; set; }
        
        [Display(Name = "إجمالي التكلفة")]
        public decimal? TotalCost { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<ProductionCyclePond> ProductionCyclePonds { get; set; } = new List<ProductionCyclePond>();
        public virtual ICollection<FeedingRecord> FeedingRecords { get; set; } = new List<FeedingRecord>();
        public virtual ICollection<MortalityRecord> MortalityRecords { get; set; } = new List<MortalityRecord>();
        public virtual ICollection<WaterQualityRecord> WaterQualityRecords { get; set; } = new List<WaterQualityRecord>();
        public virtual ICollection<CostRecord> CostRecords { get; set; } = new List<CostRecord>();
    }
}