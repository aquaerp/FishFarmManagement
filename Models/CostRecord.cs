using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج سجل التكلفة - يحتوي على جميع المعلومات المطلوبة لتسجيل التكاليف
    /// Cost Record Model - Contains all required information for cost recording
    /// </summary>
    public class CostRecord
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "الوصف")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }
        
        [Required]
        [Display(Name = "فئة التكلفة")]
        public CostCategory Category { get; set; }
        
        [Required]
        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; }
        
        [Display(Name = "نوع التكلفة")]
        public CostType CostType { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الفاتورة")]
        public string? InvoiceNumber { get; set; }
        
        [Display(Name = "تاريخ الفاتورة")]
        public DateTime? InvoiceDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اسم المورد")]
        public string? SupplierName { get; set; }
        
        [Display(Name = "رقم المستند")]
        public string? DocumentNumber { get; set; }
        
        [Display(Name = "مرجع الدفع")]
        public string? PaymentReference { get; set; }
        
        [Display(Name = "طريقة الدفع")]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Display(Name = "حالة الدفع")]
        public PaymentStatus PaymentStatus { get; set; }
        
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Display(Name = "المورد")]
        public int? SupplierId { get; set; }
        
        [Display(Name = "دورة الإنتاج")]
        public int? ProductionCycleId { get; set; }
        
        [Display(Name = "البركة")]
        public int? PondId { get; set; }
        
        [Display(Name = "الكمية")]
        public decimal? Quantity { get; set; }
        
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string? Unit { get; set; }
        
        [Display(Name = "سعر الوحدة")]
        public decimal? UnitPrice { get; set; }
        
        [Display(Name = "تاريخ استحقاق السداد")]
        public DateTime? PaymentDueDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "سجل بواسطة")]
        public string? RecordedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ProductionCycle? ProductionCycle { get; set; }
        public virtual Pond? Pond { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}