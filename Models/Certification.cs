using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج الشهادة - يحتوي على جميع المعلومات المطلوبة لإدارة الشهادات
    /// Certification Model - Contains all required information for certification management
    /// </summary>
    public class Certification
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم الشهادة")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? CertificateName { get; set; }
        
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "نوع الشهادة")]
        public CertificationType Type { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "الهيئة المصدرة")]
        public string IssuingAuthority { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "تاريخ الإصدار")]
        public DateTime IssueDate { get; set; }
        
        [Display(Name = "تاريخ الانتهاء")]
        public DateTime? ExpiryDate { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public CertificationStatus Status { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم الشهادة")]
        public string? CertificateNumber { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الملف المرفق")]
        public string? AttachmentPath { get; set; }
        
        [Display(Name = "تاريخ التجديد")]
        public DateTime? RenewalDate { get; set; }
        
        [Display(Name = "مدة الشهادة بالأيام")]
        public int? ValidityPeriodDays { get; set; }
        
        [Display(Name = "رسوم الشهادة")]
        public decimal? CertificationFee { get; set; }
        
        public decimal? CertificationCost { get; set; }
        
        public decimal? RenewalCost { get; set; }
        
        public decimal? AnnualMaintenanceCost { get; set; }
        
        public DateTime? LastAuditDate { get; set; }
        
        public DateTime? NextAuditDate { get; set; }
        
        [StringLength(50)]
        public string? LastAuditResult { get; set; }
        
        public int? AuditScore { get; set; }
        
        public int? MajorNonConformities { get; set; }
        
        public int? MinorNonConformities { get; set; }
        
        public int? CriticalNonConformities { get; set; }
        
        [StringLength(500)]
        public string? ApplicableProducts { get; set; }
        
        public bool CorrectiveActionsRequired { get; set; }
        
        [StringLength(2000)]
        public string? CorrectiveActionsPlan { get; set; }
        
        public DateTime? CorrectiveActionsDeadline { get; set; }
        
        public bool CorrectiveActionsCompleted { get; set; }
        
        public DateTime? CorrectiveActionsCompletionDate { get; set; }
        
        public DateTime? RenewalApplicationDate { get; set; }
        
        public DateTime? RenewalInspectionDate { get; set; }
        
        public bool RenewalInProgress { get; set; }
        
        [StringLength(200)]
        public string? RenewalStatus { get; set; }
        
        [StringLength(500)]
        public string? CertificateFilePath { get; set; }
        
        [StringLength(500)]
        public string? AuditReportFilePath { get; set; }
        
        [StringLength(100)]
        public string? ResponsiblePerson { get; set; }
        
        [StringLength(100)]
        public string? Department { get; set; }
        
        [StringLength(100)]
        public string? ContactPerson { get; set; }
        
        [StringLength(100)]
        public string? ContactPhone { get; set; }
        
        [StringLength(100)]
        public string? ContactEmail { get; set; }
        
        [StringLength(100)]
        public string? AuditorName { get; set; }
        
        [StringLength(100)]
        public string? AuditorOrganization { get; set; }
        
        [StringLength(100)]
        public string? AuthorityWebsite { get; set; }
        
        [StringLength(100)]
        public string? AuthorityCountry { get; set; }
        
        [StringLength(50)]
        public string? StandardVersion { get; set; }
        
        [StringLength(500)]
        public string? Scope { get; set; }
        
        [StringLength(1000)]
        public string? ComplianceRequirements { get; set; }
        
        [StringLength(2000)]
        public string? NonConformityDetails { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Display(Name = "دورة الإنتاج")]
        public int? ProductionCycleId { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "تم التحديث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ProductionCycle? ProductionCycle { get; set; }
        
        // Helper methods
        public bool IsExpired()
        {
            return ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
        }
        
        public bool IsExpiringSoon(int daysThreshold = 30)
        {
            return ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Now.AddDays(daysThreshold);
        }
        
        public bool ExpiringWithin30Days()
        {
            return IsExpiringSoon(30);
        }
        
        public int? DaysUntilExpiry()
        {
            if (!ExpiryDate.HasValue) return null;
            return (int)(ExpiryDate.Value - DateTime.Now).TotalDays;
        }
    }
}