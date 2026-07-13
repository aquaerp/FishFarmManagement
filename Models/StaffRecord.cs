using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class StaffRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string RecordType { get; set; } = string.Empty;
        
        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Now;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; } = "Active";
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        [Display(Name = "الدور")]
        public string Role { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "تاريخ التوظيف")]
        public DateTime HireDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "التدريب")]
        public string? Training { get; set; }
        
        [StringLength(200)]
        [Display(Name = "الشهادة")]
        public string? Certificate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "سجل بواسطة")]
        public string? RecordedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
    }
}