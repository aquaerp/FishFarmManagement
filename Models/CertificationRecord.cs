using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class CertificationRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int CertificationId { get; set; }
        
        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(200)]
        public string RecordType { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? CertificateName { get; set; }
        
        [StringLength(100)]
        public string? CertificateNumber { get; set; }
        
        [StringLength(200)]
        public string? Issuer { get; set; }
        
        public DateTime? IssueDate { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        
        public int? RecordedBy { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; } = "Active";
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Certification Certification { get; set; } = null!;
    }
}