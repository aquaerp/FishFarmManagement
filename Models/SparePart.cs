using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class SparePart
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PartNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public decimal UnitCost { get; set; }
        
        [Required]
        public int StockQuantity { get; set; }
        
        [Required]
        public int MinimumStock { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; } = "Active";
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Display(Name = "المعدة")]
        public int? EquipmentId { get; set; }
        
        [Display(Name = "الكمية في المخزن")]
        public int QuantityInStock { get; set; }
        
        [Display(Name = "الحد الأدنى")]
        public int MinimumQuantity { get; set; }
        
        [Display(Name = "سعر الوحدة")]
        public decimal UnitPrice { get; set; }
        
        [StringLength(200)]
        [Display(Name = "المورد")]
        public string? Supplier { get; set; }
        
        [StringLength(200)]
        [Display(Name = "الموقع")]
        public string? Location { get; set; }
        
        [Display(Name = "تاريخ آخر شراء")]
        public DateTime? LastPurchaseDate { get; set; }
        
        [Display(Name = "حرج")]
        public bool IsCritical { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Equipment? Equipment { get; set; }
    }
}