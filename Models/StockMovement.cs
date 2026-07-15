using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج بيانات حركة المخزون
    /// </summary>
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InventoryItemId { get; set; }
        [ForeignKey(nameof(InventoryItemId))]
        public InventoryItem InventoryItem { get; set; } = null!;

        [Required]
        public DateTime MovementDate { get; set; } = DateTime.Now;

        [Required]
        public StockMovementType MovementType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal BalanceAfter { get; set; }

        [StringLength(200)]
        public string? ReferenceNumber { get; set; }

        [StringLength(100)]
        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public int? ProductionCycleId { get; set; }
        [ForeignKey(nameof(ProductionCycleId))]
        public ProductionCycle? ProductionCycle { get; set; }

        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public bool IsApproved { get; set; } = false;

        [StringLength(100)]
        public string? ApprovedByUsername { get; set; }

        [StringLength(500)]
        public string? ApprovalReason { get; set; }

        public int? ApprovedById { get; set; }
        [ForeignKey(nameof(ApprovedById))]
        public Employee? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }
        
        public DateTime? ApprovedAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CreatedBy { get; set; }
        
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
        
        // خصائص إضافية مطلوبة من FishFarmContext
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? Reference { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsRejected { get; set; } = false;
        public bool IsCancelled { get; set; } = false;
        
        [StringLength(500)]
        public string? RejectionReason { get; set; }
        
        public DateTime? RejectedAt { get; set; }
        
        public int? RejectedById { get; set; }
        
        public void Reject(string reason, int rejectedBy)
        {
            IsRejected = true;
            RejectionReason = reason;
            RejectedAt = DateTime.Now;
            RejectedById = rejectedBy;
        }
        
        public string GetMovementTypeDisplay()
        {
            return MovementType switch
            {
                StockMovementType.Purchase => "شراء",
                StockMovementType.Sale => "بيع",
                StockMovementType.Production => "إنتاج",
                StockMovementType.Consumption => "استهلاك",
                StockMovementType.Transfer => "تحويل",
                StockMovementType.Adjustment => "تسوية",
                _ => MovementType.ToString()
            };
        }
    }

    /// <summary>
    /// أنواع حركة المخزون
    /// </summary>
    public enum StockMovementType
    {
        Purchase = 1,           // شراء
        Sale = 2,              // بيع
        Production = 3,        // إنتاج
        Consumption = 4,       // استهلاك
        Transfer = 5,          // تحويل
        Adjustment = 6,        // تسوية
        AdjustmentIncrease = 7, // تسوية زيادة
        AdjustmentDecrease = 8, // تسوية نقص
        Return = 9,            // إرجاع
        Damage = 10,            // تلف
        Expiry = 11,            // انتهاء صلاحية
        Expired = 12,           // منتهي الصلاحية
        Loss = 13,             // فقدان
        Waste = 14,            // هدر
        Found = 15,            // عثور
        Opening = 16,          // رصيد افتتاحي
        Closing = 17           // رصيد ختامي
    }
}
