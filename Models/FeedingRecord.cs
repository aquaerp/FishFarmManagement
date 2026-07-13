using System;
using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class FeedingRecord
    {
        public int Id { get; set; }
        
        public int CycleId { get; set; }
        public ProductionCycle Cycle { get; set; } = null!;
        
        [Required]
        public DateTime FeedingDate { get; set; }
        
        public FeedType FeedType { get; set; }
        public double Quantity { get; set; } // الكمية بالكجم
        public double FeedPrice { get; set; } // سعر الكجم
        
        public int FeedingTimes { get; set; } // عدد مرات التغذية
        
        // بيانات الأسماك وقت التغذية
        public double? EstimatedFishWeight { get; set; } // الوزن المقدر
        public int? EstimatedFishCount { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public FeedingStatus Status { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        
        public string RecordedBy { get; set; } = string.Empty; // من سجل البيانات
        public DateTime CreatedAt { get; set; }
    }

    public enum FeedType
    {
        Starter = 1,
        Grower = 2,
        Finisher = 3
    }
}
