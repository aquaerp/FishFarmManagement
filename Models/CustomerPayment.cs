using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class CustomerPayment
    {
        public int Id { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        
        public int? SalesOrderId { get; set; }
        
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
        
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }
        
        [StringLength(100)]
        public string? BankName { get; set; }
        
        [StringLength(100)]
        public string? ReceivedBy { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; } = "Completed";
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual SalesOrder? SalesOrder { get; set; }
    }
}