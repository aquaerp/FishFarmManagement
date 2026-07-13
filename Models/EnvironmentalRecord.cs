using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج السجل البيئي - يحتوي على جميع المعلومات المطلوبة للسجلات البيئية
    /// Environmental Record Model - Contains all required information for environmental records
    /// </summary>
    public class EnvironmentalRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Display(Name = "دورة الإنتاج")]
        public int? ProductionCycleId { get; set; }
        
        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Now;
        
        [Required]
        [Display(Name = "درجة الحرارة")]
        public decimal Temperature { get; set; }
        
        [Required]
        [Display(Name = "الرطوبة")]
        public decimal Humidity { get; set; }
        
        [Required]
        [Display(Name = "سرعة الرياح")]
        public decimal WindSpeed { get; set; }
        
        [StringLength(100)]
        [Display(Name = "حالة الطقس")]
        public string? WeatherCondition { get; set; }
        
        [StringLength(100)]
        [Display(Name = "سجل بواسطة")]
        public string? RecordedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "راجع بواسطة")]
        public string? ReviewedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "حدث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        [Display(Name = "الحالة")]
        public string? Status { get; set; } = "Active";
        
        [Display(Name = "دورة الإنتاج")]
        public int? CycleId { get; set; }
        
        [Display(Name = "التاريخ")]
        public DateTime? Date { get; set; }
        
        [StringLength(200)]
        [Display(Name = "المعامل")]
        public string? Parameter { get; set; }
        
        [Display(Name = "القيمة")]
        public decimal? Value { get; set; }
        
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string? Unit { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Pond Pond { get; set; } = null!;
        public virtual ProductionCycle? Cycle { get; set; }
    }
}