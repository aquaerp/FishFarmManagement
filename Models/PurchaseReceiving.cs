using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج استلام المشتريات
    /// Purchase Receiving Model
    /// </summary>
    public class PurchaseReceiving
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الاستلام")]
        public string ReceivingNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "أمر الشراء")]
        public int PurchaseOrderId { get; set; }
        
        [Required]
        [Display(Name = "تاريخ الاستلام")]
        public DateTime ReceivingDate { get; set; } = DateTime.Now;
        
        [Display(Name = "استلام كامل")]
        public bool IsFullReceiving { get; set; } = false;
        
        [Display(Name = "استلام جزئي")]
        public bool IsPartialReceiving { get; set; } = false;
        
        [Display(Name = "نتيجة فحص الجودة")]
        public QualityTestResult OverallQualityResult { get; set; } = QualityTestResult.Pending;
        
        [Display(Name = "فحص جودة مكتمل")]
        public bool QualityInspectionCompleted { get; set; } = false;
        
        [StringLength(100)]
        [Display(Name = "فحص بواسطة")]
        public string? InspectedBy { get; set; }
        
        [Display(Name = "تاريخ الفحص")]
        public DateTime? InspectionDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات الفحص")]
        public string? InspectionNotes { get; set; }
        
        [Display(Name = "درجة الحرارة عند الاستلام")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? ReceivingTemperature { get; set; }
        
        [StringLength(200)]
        [Display(Name = "حالة التغليف")]
        public string? PackagingCondition { get; set; }
        
        [Display(Name = "تلف في الشحنة")]
        public bool HasDamage { get; set; } = false;
        
        [StringLength(1000)]
        [Display(Name = "وصف التلف")]
        public string? DamageDescription { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم فاتورة المورد")]
        public string? SupplierInvoiceNumber { get; set; }
        
        [Display(Name = "تاريخ فاتورة المورد")]
        public DateTime? SupplierInvoiceDate { get; set; }
        
        [Display(Name = "مبلغ فاتورة المورد")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SupplierInvoiceAmount { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم سند الاستلام")]
        public string? GoodsReceivedNumber { get; set; }
        
        [StringLength(100)]
        [Display(Name = "مُستلِم")]
        public string? ReceivedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "موافق بواسطة")]
        public string? ApprovedBy { get; set; }
        
        [Display(Name = "تاريخ الموافقة")]
        public DateTime? ApprovedDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(100)]
        [Display(Name = "حُدّث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        // Navigation properties
        [ForeignKey(nameof(PurchaseOrderId))]
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
        
        public virtual ICollection<PurchaseReceivingItem> Items { get; set; } = new List<PurchaseReceivingItem>();
        
        // Computed Properties
        [NotMapped]
        public bool IsApproved => ApprovedDate.HasValue && !string.IsNullOrEmpty(ApprovedBy);
        
        [NotMapped]
        public decimal TotalReceivedQuantity => Items?.Sum(i => i.ReceivedQuantity) ?? 0;
        
        [NotMapped]
        public int ItemsCount => Items?.Count ?? 0;
        
        [NotMapped]
        public bool AllItemsPassedQuality => Items?.All(i => i.QualityAccepted) ?? false;
    }
    
    /// <summary>
    /// نموذج بند استلام المشتريات
    /// Purchase Receiving Item Model
    /// </summary>
    public class PurchaseReceivingItem
    {
        public int Id { get; set; }
        
        [Required]
        public int PurchaseReceivingId { get; set; }
        
        [Required]
        public int PurchaseOrderItemId { get; set; }
        
        [Required]
        public int InventoryItemId { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم العنصر")]
        public string ItemName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "الكمية المطلوبة")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal OrderedQuantity { get; set; }
        
        [Required]
        [Display(Name = "الكمية المستلمة")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal ReceivedQuantity { get; set; }
        
        [Display(Name = "الكمية المرفوضة")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal RejectedQuantity { get; set; }
        
        [Display(Name = "سعر الوحدة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
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
        [Display(Name = "سبب الرفض")]
        public string? RejectionReason { get; set; }
        
        [Display(Name = "تم إضافة للمخزون")]
        public bool AddedToStock { get; set; } = false;
        
        [Display(Name = "تاريخ الإضافة للمخزون")]
        public DateTime? StockAddedDate { get; set; }
        
        [Display(Name = "حركة مخزون")]
        public int? StockMovementId { get; set; }
        
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey(nameof(PurchaseReceivingId))]
        public virtual PurchaseReceiving PurchaseReceiving { get; set; } = null!;
        
        [ForeignKey(nameof(PurchaseOrderItemId))]
        public virtual PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;
        
        [ForeignKey(nameof(InventoryItemId))]
        public virtual InventoryItem InventoryItem { get; set; } = null!;
        
        [ForeignKey(nameof(StockMovementId))]
        public virtual StockMovement? StockMovement { get; set; }
        
        // Computed Properties
        [NotMapped]
        public decimal AcceptedQuantity => ReceivedQuantity - RejectedQuantity;
        
        [NotMapped]
        public bool IsFullyReceived => ReceivedQuantity >= OrderedQuantity;
        
        [NotMapped]
        public decimal ReceivedPercentage => OrderedQuantity > 0 ? (ReceivedQuantity / OrderedQuantity) * 100 : 0;
    }
}


