using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class FishHealthRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(200)]
        public string HealthStatus { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Symptoms { get; set; }
        
        [StringLength(1000)]
        public string? Diagnosis { get; set; }
        
        [StringLength(1000)]
        public string? Treatment { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public FishHealthStatus Status { get; set; }
        
        [Display(Name = "دورة الإنتاج")]
        public int? CycleId { get; set; }
        
        [Display(Name = "التاريخ")]
        public DateTime? Date { get; set; }
        
        [StringLength(200)]
        [Display(Name = "المرض")]
        public string? Disease { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "الإجراء المتخذ")]
        public string? ActionTaken { get; set; }
        
        [StringLength(100)]
        [Display(Name = "سجل بواسطة")]
        public string? RecordedBy { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Pond Pond { get; set; } = null!;
        public virtual ProductionCycle? Cycle { get; set; }
    }
}