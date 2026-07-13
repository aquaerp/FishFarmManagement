using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class SupplierPayment
    {
        public int Id { get; set; }
        
        [Required]
        public int SupplierId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Reference { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; } = "Pending";
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم المرجع")]
        public string? ReferenceNumber { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اسم البنك")]
        public string? BankName { get; set; }
        
        [StringLength(100)]
        [Display(Name = "دفع بواسطة")]
        public string? PaidBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
    }
}

