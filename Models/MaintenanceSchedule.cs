using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج جدولة الصيانة - يحتوي على جميع المعلومات المطلوبة لجدولة الصيانة
    /// Maintenance Schedule Model - Contains all required information for maintenance scheduling
    /// </summary>
    public class MaintenanceSchedule
    {
        public int Id { get; set; }
        
        [Required]
        public int EquipmentId { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "نوع الصيانة")]
        public string MaintenanceType { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "التكرار")]
        public string Frequency { get; set; } = string.Empty;
        
        [Display(Name = "عدد الأيام")]
        public int? FrequencyDays { get; set; }
        
        [Display(Name = "تاريخ الصيانة الأخيرة")]
        public DateTime? LastMaintenanceDate { get; set; }
        
        [Display(Name = "تاريخ الصيانة التالية")]
        public DateTime? NextMaintenanceDate { get; set; }
        
        [Required]
        public DateTime ScheduledDate { get; set; }
        
        [Display(Name = "الأولوية")]
        public int Priority { get; set; }
        
        [Display(Name = "التكلفة المقدرة")]
        public decimal? EstimatedCost { get; set; }
        
        [Display(Name = "المدة المقدرة (دقيقة)")]
        public int? EstimatedDurationMinutes { get; set; }
        
        [StringLength(100)]
        [Display(Name = "مكلف إلى")]
        public string? AssignedTo { get; set; }
        
        [StringLength(500)]
        [Display(Name = "الأجزاء المطلوبة")]
        public string? RequiredParts { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "تعليمات الصيانة")]
        public string? MaintenanceInstructions { get; set; }
        
        [Display(Name = "إرسال إشعار")]
        public bool SendNotification { get; set; }
        
        [Display(Name = "عدد الأيام قبل الإشعار")]
        public int? NotificationDaysBefore { get; set; }
        
        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;
        
        [StringLength(1000)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }
        
        [StringLength(50)]
        [Display(Name = "الحالة")]
        public string? Status { get; set; } = "Scheduled";
        
        [StringLength(100)]
        [Display(Name = "حدث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        
        // Navigation properties
        public virtual Equipment Equipment { get; set; } = null!;
    }
}