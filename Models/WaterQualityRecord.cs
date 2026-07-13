using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class WaterQualityRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        public DateTime RecordDate { get; set; }
        
        [Required]
        public decimal Temperature { get; set; }
        
        [Required]
        public decimal pH { get; set; }
        
        [Required]
        public decimal DissolvedOxygen { get; set; }
        
        [Required]
        public decimal Ammonia { get; set; }
        
        [Required]
        public decimal Nitrite { get; set; }
        
        [Required]
        public decimal Nitrate { get; set; }
        
        public decimal? Salinity { get; set; }
        
        public decimal? Turbidity { get; set; }
        
        public decimal? Alkalinity { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public WaterQualityStatus Status { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Display(Name = "دورة الإنتاج")]
        public int? CycleId { get; set; }
        
        [Display(Name = "تاريخ القياس")]
        public DateTime? MeasurementDate { get; set; }
        
        [Display(Name = "تم التسجيل بواسطة")]
        public int? RecordedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Pond Pond { get; set; } = null!;
        public virtual ProductionCycle? Cycle { get; set; }
        public virtual Employee? RecordedByEmployee { get; set; }
    }
}