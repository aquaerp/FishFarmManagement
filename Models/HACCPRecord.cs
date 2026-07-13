using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج سجل HACCP - يحتوي على جميع المعلومات المطلوبة لتسجيل نقاط التحكم الحرجة
    /// HACCP Record Model - Contains all required information for HACCP record management
    /// </summary>
    public class HACCPRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم السجل")]
        public string RecordNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime RecordDate { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "نوع الخطر")]
        public string HazardType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "نقطة التحكم")]
        public string ControlPoint { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "وصف نقطة التحكم")]
        public string? ControlPointDescription { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "وصف الخطر")]
        public string HazardDescription { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "الخطورة")]
        public HazardSeverity Severity { get; set; }
        
        [Required]
        [Display(Name = "الاحتمالية")]
        public HazardLikelihood Likelihood { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "طريقة المراقبة")]
        public string MonitoringMethod { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "التكرار")]
        public MonitoringFrequency Frequency { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "وحدة القياس")]
        public string MeasurementUnit { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "الحد الأدنى")]
        public decimal MinimumLimit { get; set; }
        
        [Required]
        [Display(Name = "الحد الأقصى")]
        public decimal MaximumLimit { get; set; }
        
        [Required]
        [Display(Name = "القيمة المستهدفة")]
        public decimal TargetValue { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "معايير القبول")]
        public string AcceptanceCriteria { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "القيمة الفعلية")]
        public decimal ActualValue { get; set; }
        
        [Required]
        [Display(Name = "وقت القياس")]
        public DateTime MeasurementTime { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public ComplianceStatus Status { get; set; }
        
        [Required]
        [Display(Name = "ضمن الحدود")]
        public bool IsWithinLimits { get; set; }
        
        [Display(Name = "حدث انحراف")]
        public bool DeviationOccurred { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "وصف الانحراف")]
        public string? DeviationDescription { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "الإجراءات التصحيحية")]
        public string? CorrectiveActions { get; set; }
        
        [Display(Name = "تاريخ اتخاذ الإجراء")]
        public DateTime? ActionTakenDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اتخذ الإجراء بواسطة")]
        public string? ActionTakenBy { get; set; }
        
        [Display(Name = "تم التحقق")]
        public bool Verified { get; set; }
        
        [StringLength(100)]
        [Display(Name = "تحقق بواسطة")]
        public string? VerifiedBy { get; set; }
        
        [Display(Name = "تاريخ التحقق")]
        public DateTime? VerificationDate { get; set; }
        
        [StringLength(200)]
        [Display(Name = "المرجع")]
        public string? ReferenceDocument { get; set; }
        
        [StringLength(200)]
        [Display(Name = "المعدات المستخدمة")]
        public string? EquipmentUsed { get; set; }
        
        [StringLength(200)]
        [Display(Name = "الموقع")]
        public string? Location { get; set; }
        
        [StringLength(100)]
        [Display(Name = "سجل بواسطة")]
        public string? RecordedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "راجع بواسطة")]
        public string? ReviewedBy { get; set; }
        
        [Display(Name = "تاريخ المراجعة")]
        public DateTime? ReviewDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "حدث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [Display(Name = "مستوى المخاطر")]
        public string? RiskLevel { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Pond Pond { get; set; } = null!;
    }
}