using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج سجل الصيانة - يحتوي على جميع المعلومات المطلوبة لتسجيل أعمال الصيانة
    /// Maintenance Record Model - Contains all required information for maintenance record management
    /// </summary>
    public class MaintenanceRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int EquipmentId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم السجل")]
        public string RecordNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime MaintenanceDate { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "نوع الصيانة")]
        public string MaintenanceType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "الوصف")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        [Display(Name = "وصف المشكلة")]
        public string ProblemDescription { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        [Display(Name = "العمل المنجز")]
        public string WorkPerformed { get; set; } = string.Empty;
        
        [StringLength(1000)]
        [Display(Name = "الأجزاء المستبدلة")]
        public string? PartsReplaced { get; set; }
        
        [Display(Name = "تكلفة الأجزاء")]
        public decimal PartsCost { get; set; }
        
        [Display(Name = "تكلفة العمالة")]
        public decimal LaborCost { get; set; }
        
        [StringLength(100)]
        [Display(Name = "الفني")]
        public string? Technician { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "نفذ بواسطة")]
        public string PerformedBy { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "يتطلب متابعة")]
        public bool RequiresFollowUp { get; set; }
        
        [Display(Name = "تاريخ المتابعة")]
        public DateTime? FollowUpDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Display(Name = "التكلفة الإجمالية")]
        public decimal? TotalCost { get; set; }
        
        [StringLength(50)]
        [Display(Name = "الحالة")]
        public string? Status { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        
        // Navigation properties
        public virtual Equipment Equipment { get; set; } = null!;
    }
}