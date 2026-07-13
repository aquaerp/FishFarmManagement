using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج إجازة الموظف - يحتوي على جميع المعلومات المطلوبة لإدارة إجازات الموظفين
    /// Employee Leave Model - Contains all required information for employee leave management
    /// </summary>
    public class EmployeeLeave
    {
        public int Id { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الإجازة")]
        public string LeaveNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "تاريخ الطلب")]
        public DateTime RequestDate { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "طلب بواسطة")]
        public string RequestedBy { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "نوع الإجازة")]
        public LeaveType LeaveType { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public LeaveStatus Status { get; set; }
        
        [Required]
        [Display(Name = "تاريخ البداية")]
        public DateTime StartDate { get; set; }
        
        [Required]
        [Display(Name = "تاريخ النهاية")]
        public DateTime EndDate { get; set; }
        
        [Required]
        [Display(Name = "عدد الأيام")]
        public int DaysCount { get; set; }
        
        [Required]
        [Display(Name = "مدفوعة")]
        public bool IsPaid { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "السبب")]
        public string? Reason { get; set; }
        
        [Display(Name = "تاريخ الموافقة")]
        public DateTime? ApprovalDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "وافق بواسطة")]
        public string? ApprovedBy { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "سبب الرفض")]
        public string? RejectionReason { get; set; }
        
        [Display(Name = "المدة")]
        public int? Duration { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
    }
}