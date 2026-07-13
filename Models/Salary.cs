using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج الراتب - يحتوي على جميع المعلومات المطلوبة لمعالجة الرواتب
    /// Salary Model - Contains all required information for salary processing
    /// </summary>
    public class Salary
    {
        public int Id { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الراتب")]
        public string SalaryNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "الشهر")]
        public int Month { get; set; }
        
        [Required]
        [Display(Name = "السنة")]
        public int Year { get; set; }
        
        [Required]
        public DateTime PayPeriodStart { get; set; }
        
        [Required]
        public DateTime PayPeriodEnd { get; set; }
        
        [Required]
        [Display(Name = "الراتب الأساسي")]
        public decimal BasicSalary { get; set; }
        
        [Display(Name = "بدل السكن")]
        public decimal HousingAllowance { get; set; }
        
        [Display(Name = "بدل النقل")]
        public decimal TransportationAllowance { get; set; }
        
        [Display(Name = "بدل الطعام")]
        public decimal FoodAllowance { get; set; }
        
        [Display(Name = "بدلات أخرى")]
        public decimal OtherAllowances { get; set; }
        
        [Display(Name = "أيام العمل")]
        public int WorkDays { get; set; }
        
        [Display(Name = "أيام العمل المتوقعة")]
        public int ExpectedWorkDays { get; set; }
        
        [Display(Name = "ساعات العمل الإضافي")]
        public decimal OvertimeHours { get; set; }
        
        [Display(Name = "معدل العمل الإضافي")]
        public decimal OvertimeRate { get; set; }
        
        [Display(Name = "إجمالي المكافآت")]
        public decimal TotalBonuses { get; set; }
        
        [Display(Name = "إجمالي العمولات")]
        public decimal TotalCommissions { get; set; }
        
        [Display(Name = "أيام الغياب")]
        public int AbsenceDays { get; set; }
        
        [Display(Name = "خصم الغياب")]
        public decimal AbsenceDeduction { get; set; }
        
        [Display(Name = "أيام التأخير")]
        public int LateDays { get; set; }
        
        [Display(Name = "خصم التأخير")]
        public decimal LateDeduction { get; set; }
        
        [Display(Name = "خصم التأمين الاجتماعي")]
        public decimal SocialInsuranceDeduction { get; set; }
        
        [Display(Name = "خصم التأمين الصحي")]
        public decimal HealthInsuranceDeduction { get; set; }
        
        [Display(Name = "إجمالي الخصومات الأخرى")]
        public decimal TotalOtherDeductions { get; set; }
        
        [Display(Name = "السلفة")]
        public decimal Loan { get; set; }
        
        [Display(Name = "السلفة المقدمة")]
        public decimal Advance { get; set; }
        
        [Display(Name = "إجمالي الراتب")]
        public decimal GrossSalary { get; set; }
        
        [Display(Name = "إجمالي الخصومات")]
        public decimal TotalDeductions { get; set; }
        
        [Required]
        [Display(Name = "صافي الراتب")]
        public decimal NetSalary { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public SalaryStatus Status { get; set; }
        
        [Display(Name = "طريقة الدفع")]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Display(Name = "تاريخ الدفع")]
        public DateTime? PaidDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "مرجع الدفع")]
        public string? PaymentReference { get; set; }
        
        [StringLength(100)]
        [Display(Name = "أعد بواسطة")]
        public string? PreparedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "وافق بواسطة")]
        public string? ApprovedBy { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        
        // Helper methods
        public decimal CalculateOvertimeAmount()
        {
            return OvertimeHours * OvertimeRate;
        }
        
        public decimal CalculateAbsenceDeduction()
        {
            if (ExpectedWorkDays == 0) return 0;
            return (AbsenceDays / (decimal)ExpectedWorkDays) * BasicSalary;
        }
        
        public decimal CalculateNetSalary()
        {
            var gross = BasicSalary + HousingAllowance + TransportationAllowance + 
                       FoodAllowance + OtherAllowances + CalculateOvertimeAmount() + 
                       TotalBonuses + TotalCommissions;
            
            var deductions = AbsenceDeduction + LateDeduction + SocialInsuranceDeduction + 
                           HealthInsuranceDeduction + TotalOtherDeductions + Loan + Advance;
            
            return gross - deductions;
        }
    }
}