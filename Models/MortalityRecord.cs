using System;
using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class MortalityRecord
    {
        public int Id { get; set; }
        
        public int CycleId { get; set; }
        public ProductionCycle Cycle { get; set; } = null!;
        
        [Required]
        public DateTime Date { get; set; }
        
        public int DeadFishCount { get; set; }
        public double? AverageWeight { get; set; } // متوسط وزن الأسماك النافقة
        
        public MortalityCause Cause { get; set; }
        public string CauseDescription { get; set; } = string.Empty;
        
        // الإجراءات المتخذة
        public string ActionTaken { get; set; } = string.Empty;
        public string Treatment { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "الحالة")]
        public MortalityStatus Status { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        public string PhotoPath { get; set; } = string.Empty; // مسار الصورة
        
        public string RecordedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    
    public enum MortalityCause
    {
        Disease = 1,        // مرض
        WaterQuality = 2,   // جودة المياه
        Stress = 3,         // إجهاد
        Predation = 4,      // افتراس
        Unknown = 5,        // غير معروف
        Handling = 6,       // سوء التعامل
        Temperature = 7     // درجة الحرارة
    }
}
