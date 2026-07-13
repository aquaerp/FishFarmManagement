using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج بند أمر الشراء
    /// Purchase Order Item Model
    /// </summary>
    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "أمر الشراء")]
        public int PurchaseOrderId { get; set; }
        
        [Required]
        [Display(Name = "عنصر المخزون")]
        public int InventoryItemId { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم العنصر")]
        public string ItemName { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "الكمية المطلوبة")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
        
        [Display(Name = "الكمية المستلمة")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantity { get; set; }
        
        [Display(Name = "الكمية المتبقية")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal RemainingQuantity { get; set; }
        
        [Required]
        [Display(Name = "سعر الوحدة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Display(Name = "نسبة الخصم")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }
        
        [Display(Name = "مبلغ الخصم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }
        
        [Required]
        [Display(Name = "الإجمالي")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        [StringLength(50)]
        [Display(Name = "رقم الدفعة")]
        public string? BatchNumber { get; set; }
        
        [Display(Name = "تاريخ الصلاحية")]
        public DateTime? ExpiryDate { get; set; }
        
        [Display(Name = "جودة مقبولة")]
        public bool QualityAccepted { get; set; } = true;
        
        [Display(Name = "نتيجة فحص الجودة")]
        public QualityTestResult? QualityResult { get; set; }
        
        [StringLength(500)]
        [Display(Name = "ملاحظات الجودة")]
        public string? QualityNotes { get; set; }
        
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey(nameof(PurchaseOrderId))]
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
        
        [ForeignKey(nameof(InventoryItemId))]
        public virtual InventoryItem InventoryItem { get; set; } = null!;
        
        // Computed Properties
        [NotMapped]
        public bool IsFullyReceived => ReceivedQuantity >= Quantity;
        
        [NotMapped]
        public decimal ReceivedPercentage => Quantity > 0 ? (ReceivedQuantity / Quantity) * 100 : 0;
        
        [NotMapped]
        public bool HasQualityIssue => !QualityAccepted || QualityResult == QualityTestResult.Failed;
        
        // Helper Methods
        public void CalculateTotalPrice()
        {
            DiscountAmount = (UnitPrice * Quantity) * (DiscountPercentage / 100);
            TotalPrice = (UnitPrice * Quantity) - DiscountAmount;
        }
        
        public void UpdateRemainingQuantity()
        {
            RemainingQuantity = Quantity - ReceivedQuantity;
        }
    }
}