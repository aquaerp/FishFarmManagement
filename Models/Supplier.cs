using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? ContactPerson { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(20)]
        public string? Fax { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(20)]
        public string? PostalCode { get; set; }
        
        [StringLength(50)]
        public string? TaxNumber { get; set; }
        
        [StringLength(50)]
        public string? CommercialRegister { get; set; }
        
        [StringLength(100)]
        public string? PaymentTerms { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public SupplierStatus Status { get; set; }
        
        [Required]
        [Display(Name = "نوع المورد")]
        public SupplierType Type { get; set; }
        
        [Display(Name = "الرصيد الحالي")]
        public decimal CurrentBalance { get; set; }
        
        [Display(Name = "حد الائتمان")]
        public decimal CreditLimit { get; set; }
        
        [Display(Name = "أيام السداد")]
        public int PaymentTermDays { get; set; }
        
        [Display(Name = "تاريخ آخر عملية")]
        public DateTime? LastPurchaseDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<CostRecord> CostRecords { get; set; } = new List<CostRecord>();
    }
}