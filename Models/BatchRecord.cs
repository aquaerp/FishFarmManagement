using System;
using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class BatchRecord
    {
        public int Id { get; set; }
        [Required]
        public string BatchType { get; set; } = string.Empty; // زريعة، أعلاف، ...
        public string Source { get; set; } = string.Empty;
        public double? Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime? ArrivalDate { get; set; }
        public int? RelatedCycleId { get; set; }
        public ProductionCycle RelatedCycle { get; set; } = null!;
        public string Notes { get; set; } = string.Empty;
        public string RecordedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
