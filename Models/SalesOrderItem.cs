using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج عنصر طلب البيع - يحتوي على جميع المعلومات المطلوبة لعناصر طلبات البيع
    /// Sales Order Item Model - Contains all required information for sales order items
    /// </summary>
    public class SalesOrderItem
    {
        public int Id { get; set; }
        
        [Required]
        public int SalesOrderId { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم المنتج")]
        public string ProductName { get; set; } = string.Empty;
        
        [Display(Name = "دورة الإنتاج")]
        public int? ProductionCycleId { get; set; }
        
        [Required]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }
        
        [Required]
        [Display(Name = "سعر الوحدة")]
        public decimal UnitPrice { get; set; }
        
        [Required]
        [Display(Name = "الدرجة")]
        public FishGrade Grade { get; set; }
        
        [Required]
        [Display(Name = "المجموع الفرعي")]
        public decimal SubTotal { get; set; }
        
        [Display(Name = "مبلغ الخصم")]
        public decimal DiscountAmount { get; set; }
        
        [Required]
        [Display(Name = "المجموع الكلي")]
        public decimal TotalPrice { get; set; }
        
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual SalesOrder SalesOrder { get; set; } = null!;
        public virtual ProductionCycle? ProductionCycle { get; set; }
    }
}