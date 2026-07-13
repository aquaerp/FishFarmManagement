using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج أمر الشراء
    /// Purchase Order Model
    /// </summary>
    public class PurchaseOrder
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الأمر")]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "تاريخ الأمر")]
        public DateTime OrderDate { get; set; } = DateTime.Now;
        
        [Required]
        [Display(Name = "تاريخ التسليم المتوقع")]
        public DateTime ExpectedDeliveryDate { get; set; }
        
        [Display(Name = "تاريخ التسليم الفعلي")]
        public DateTime? ActualDeliveryDate { get; set; }
        
        [Required]
        [Display(Name = "المورد")]
        public int SupplierId { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
        
        [Display(Name = "الإجمالي قبل الضريبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        
        [Display(Name = "نسبة الخصم")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }
        
        [Display(Name = "مبلغ الخصم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }
        
        [Display(Name = "الإجمالي بعد الخصم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountAfterDiscount { get; set; }
        
        [Display(Name = "نسبة الضريبة")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal VATRate { get; set; } = 0.15m; // 15%
        
        [Display(Name = "مبلغ الضريبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VATAmount { get; set; }
        
        [Required]
        [Display(Name = "الإجمالي النهائي")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        
        [StringLength(200)]
        [Display(Name = "شروط الدفع")]
        public string? PaymentTerms { get; set; }
        
        [Display(Name = "أجل الدفع بالأيام")]
        public int PaymentDueDays { get; set; } = 30;
        
        [Display(Name = "تاريخ استحقاق الدفع")]
        public DateTime? PaymentDueDate { get; set; }
        
        [StringLength(500)]
        [Display(Name = "عنوان التسليم")]
        public string? DeliveryAddress { get; set; }
        
        [StringLength(100)]
        [Display(Name = "جهة الاتصال")]
        public string? ContactPerson { get; set; }
        
        [StringLength(20)]
        [Display(Name = "رقم الاتصال")]
        public string? ContactPhone { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [StringLength(100)]
        [Display(Name = "القسم")]
        public string? Department { get; set; }
        
        [Display(Name = "أولوية")]
        public PurchasePriority Priority { get; set; } = PurchasePriority.Normal;
        
        [Display(Name = "تم الاستلام")]
        public bool IsReceived { get; set; } = false;
        
        [Display(Name = "استلام جزئي")]
        public bool IsPartiallyReceived { get; set; } = false;
        
        [Display(Name = "نسبة الاستلام")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal ReceivedPercentage { get; set; }
        
        // Workflow Fields
        [Display(Name = "طلب بواسطة")]
        public int? RequestedById { get; set; }
        
        [Display(Name = "تاريخ الطلب")]
        public DateTime? RequestedDate { get; set; }
        
        [Display(Name = "وافق بواسطة")]
        public int? ApprovedById { get; set; }
        
        [Display(Name = "تاريخ الموافقة")]
        public DateTime? ApprovedDate { get; set; }
        
        [Display(Name = "أرسل بواسطة")]
        public int? SentById { get; set; }
        
        [Display(Name = "تاريخ الإرسال")]
        public DateTime? SentDate { get; set; }
        
        [Display(Name = "استلم بواسطة")]
        public int? ReceivedById { get; set; }
        
        [Display(Name = "تاريخ الاستلام")]
        public DateTime? ReceivedDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "حُدّث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; } = null!;
        
        [ForeignKey(nameof(RequestedById))]
        public virtual Employee? RequestedBy { get; set; }
        
        [ForeignKey(nameof(ApprovedById))]
        public virtual Employee? ApprovedBy { get; set; }
        
        [ForeignKey(nameof(SentById))]
        public virtual Employee? SentBy { get; set; }
        
        [ForeignKey(nameof(ReceivedById))]
        public virtual Employee? ReceivedBy { get; set; }
        
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        
        // Computed Properties
        [NotMapped]
        public decimal TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;
        
        [NotMapped]
        public decimal ReceivedQuantity => Items?.Sum(i => i.ReceivedQuantity) ?? 0m;
        
        [NotMapped]
        public int ItemsCount => Items?.Count ?? 0;
        
        [NotMapped]
        public bool IsFullyReceived => Items?.All(i => i.IsFullyReceived) ?? false;
        
        [NotMapped]
        public bool CanApprove => Status == PurchaseOrderStatus.Submitted;
        
        [NotMapped]
        public bool CanSend => Status == PurchaseOrderStatus.Approved;
        
        [NotMapped]
        public bool CanReceive => Status == PurchaseOrderStatus.Sent;
        
        // Helper Methods
        public void CalculateTotals()
        {
            SubTotal = Items?.Sum(i => i.TotalPrice) ?? 0;
            AmountAfterDiscount = SubTotal - DiscountAmount;
            VATAmount = AmountAfterDiscount * VATRate;
            Total = AmountAfterDiscount + VATAmount;
            ReceivedPercentage = Items?.Any() == true 
                ? (Items.Sum(i => i.ReceivedQuantity) / Items.Sum(i => i.Quantity)) * 100 
                : 0m;
        }
        
        public string GetStatusDisplay()
        {
            return Status switch
            {
                PurchaseOrderStatus.Draft => "مسودة",
                PurchaseOrderStatus.Submitted => "مقدم",
                PurchaseOrderStatus.Approved => "موافق",
                PurchaseOrderStatus.Sent => "مُرسل",
                PurchaseOrderStatus.PartiallyReceived => "استلام جزئي",
                PurchaseOrderStatus.Received => "مُستلم",
                PurchaseOrderStatus.Closed => "مغلق",
                PurchaseOrderStatus.Cancelled => "ملغي",
                _ => Status.ToString()
            };
        }
    }
    
    /// <summary>
    /// حالات أمر الشراء
    /// Purchase Order Status
    /// </summary>
    public enum PurchaseOrderStatus
    {
        [Display(Name = "مسودة")]
        Draft = 1,
        
        [Display(Name = "مقدم")]
        Submitted = 2,
        
        [Display(Name = "موافق عليه")]
        Approved = 3,
        
        [Display(Name = "مُرسل للمورد")]
        Sent = 4,
        
        [Display(Name = "استلام جزئي")]
        PartiallyReceived = 5,
        
        [Display(Name = "مُستلم")]
        Received = 6,
        
        [Display(Name = "مغلق")]
        Closed = 7,
        
        [Display(Name = "ملغي")]
        Cancelled = 8
    }
    
    /// <summary>
    /// أولوية الشراء
    /// Purchase Priority
    /// </summary>
    public enum PurchasePriority
    {
        [Display(Name = "منخفضة")]
        Low = 1,
        
        [Display(Name = "عادية")]
        Normal = 2,
        
        [Display(Name = "عالية")]
        High = 3,
        
        [Display(Name = "عاجلة")]
        Urgent = 4
    }
}