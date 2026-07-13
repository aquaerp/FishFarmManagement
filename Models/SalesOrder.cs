using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج طلب البيع - يحتوي على جميع المعلومات المطلوبة لإدارة طلبات البيع
    /// Sales Order Model - Contains all required information for sales order management
    /// </summary>
    public class SalesOrder
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الطلب")]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "تاريخ الطلب")]
        public DateTime OrderDate { get; set; } = DateTime.Now;
        
        [Display(Name = "تاريخ التسليم المتوقع")]
        public DateTime? ExpectedDeliveryDate { get; set; }
        
        [Display(Name = "تاريخ التسليم الفعلي")]
        public DateTime? DeliveryDate { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public SalesOrderStatus Status { get; set; }
        
        [Required]
        [Display(Name = "المجموع الفرعي")]
        public decimal SubTotal { get; set; }
        
        [Display(Name = "مبلغ الخصم")]
        public decimal DiscountAmount { get; set; }
        
        [Required]
        [Display(Name = "مبلغ الضريبة")]
        public decimal TaxAmount { get; set; }
        
        [Display(Name = "مبلغ الضريبة المضافة")]
        public decimal VATAmount { get; set; }
        
        [Required]
        [Display(Name = "المجموع الكلي")]
        public decimal GrandTotal { get; set; }
        
        [Display(Name = "المبلغ المدفوع")]
        public decimal PaidAmount { get; set; }
        
        [Display(Name = "المبلغ المتبقي")]
        public decimal RemainingAmount { get; set; }
        
        [Display(Name = "المبلغ الإجمالي")]
        public decimal TotalAmount { get; set; }
        
        [StringLength(500)]
        [Display(Name = "عنوان التسليم")]
        public string? DeliveryAddress { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "تعليمات التسليم")]
        public string? DeliveryInstructions { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
    }
}