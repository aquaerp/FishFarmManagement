using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class ProductionCyclePond
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductionCycleId { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ProductionCycle ProductionCycle { get; set; } = null!;
        public virtual Pond Pond { get; set; } = null!;
    }
}