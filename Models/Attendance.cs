using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public DateTime? CheckIn { get; set; }
        
        public DateTime? CheckOut { get; set; }
        
        public decimal? HoursWorked { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public AttendanceStatus Status { get; set; }
        
        [Display(Name = "وقت الحضور")]
        public DateTime? CheckInTime { get; set; }
        
        [Display(Name = "وقت الانصراف")]
        public DateTime? CheckOutTime { get; set; }
        
        [Display(Name = "ساعات العمل")]
        public decimal? WorkedHours { get; set; }
        
        [Display(Name = "دقائق التأخير")]
        public int? LateMinutes { get; set; }
        
        [Display(Name = "دقائق الانصراف المبكر")]
        public int? EarlyLeaveMinutes { get; set; }
        
        [Display(Name = "ساعات العمل الإضافي")]
        public decimal? OvertimeHours { get; set; }
        
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