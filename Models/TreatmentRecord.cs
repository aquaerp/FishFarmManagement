using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class TreatmentRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        public DateTime TreatmentDate { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(200)]
        public string TreatmentType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string TreatmentName { get; set; } = string.Empty;
        
        [Required]
        public decimal Dosage { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; } = "Completed";
        
        [Display(Name = "دورة الإنتاج")]
        public int? CycleId { get; set; }
        
        [Display(Name = "التاريخ")]
        public DateTime? Date { get; set; }
        
        [Display(Name = "فترة الانسحاب")]
        public int? WithdrawalPeriod { get; set; }
        
        [StringLength(100)]
        [Display(Name = "سجل بواسطة")]
        public string? RecordedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Pond Pond { get; set; } = null!;
        public virtual ProductionCycle? Cycle { get; set; }
    }
}