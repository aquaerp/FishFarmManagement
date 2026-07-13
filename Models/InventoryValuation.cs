using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج بيانات تقييم المخزون
    /// </summary>
    public class InventoryValuation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InventoryItemId { get; set; }
        [ForeignKey(nameof(InventoryItemId))]
        public InventoryItem InventoryItem { get; set; } = null!;

        [Required]
        public DateTime ValuationDate { get; set; } = DateTime.Now;

        [Required]
        public ValuationMethod Method { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MarketPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NetRealizableValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WriteDownAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public bool IsApproved { get; set; } = false;

        public int? ApprovedById { get; set; }
        [ForeignKey(nameof(ApprovedById))]
        public Employee? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// طرق تقييم المخزون
    /// </summary>
    public enum ValuationMethod
    {
        FIFO = 1,              // الوارد أولاً صادر أولاً
        LIFO = 2,              // الوارد أخيراً صادر أولاً
        WeightedAverage = 3,   // المتوسط المرجح
        StandardCost = 4,      // التكلفة المعيارية
        ActualCost = 5,        // التكلفة الفعلية
        LowestCostOrMarket = 6 // التكلفة أو السوق أيهما أقل
    }
}
