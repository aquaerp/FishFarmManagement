using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(50)]
        public string? TaxNumber { get; set; }
        
        [StringLength(50)]
        public string? CommercialRegistration { get; set; }
        
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public CustomerStatus Status { get; set; }
        
        [Required]
        [Display(Name = "نوع العميل")]
        public CustomerType Type { get; set; }
        
        [Display(Name = "الرصيد الحالي")]
        public decimal CurrentBalance { get; set; }
        
        [Display(Name = "حد الائتمان")]
        public decimal CreditLimit { get; set; }
        
        [Display(Name = "أيام السداد")]
        public int PaymentTermDays { get; set; }
        
        [Display(Name = "تاريخ آخر عملية")]
        public DateTime? LastTransactionDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    }
}