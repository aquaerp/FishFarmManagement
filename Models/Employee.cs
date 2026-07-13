using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج الموظف - يحتوي على جميع المعلومات المطلوبة لإدارة الموظفين
    /// Employee Model - Contains all required information for employee management
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الموظف")]
        public string EmployeeNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(14)]
        [Display(Name = "رقم الهوية الوطنية")]
        public string NationalId { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "تاريخ الميلاد")]
        public DateTime BirthDate { get; set; }
        
        [Required]
        [Display(Name = "تاريخ التعيين")]
        public DateTime HireDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }
        
        [StringLength(20)]
        [Display(Name = "رقم الهاتف")]
        public string? Phone { get; set; }
        
        [StringLength(500)]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }
        
        [Required]
        [Display(Name = "المنصب")]
        public EmployeePosition Position { get; set; }
        
        [Required]
        [Display(Name = "القسم")]
        public EmployeeDepartment Department { get; set; }
        
        [Required]
        [Display(Name = "نوع التوظيف")]
        public EmploymentType EmploymentType { get; set; }
        
        [Required]
        [Display(Name = "الحالة")]
        public EmployeeStatus Status { get; set; }
        
        [Required]
        [Display(Name = "الراتب الأساسي")]
        public decimal BasicSalary { get; set; }
        
        [Display(Name = "بدل السكن")]
        public decimal? HousingAllowance { get; set; }
        
        [Display(Name = "بدل النقل")]
        public decimal? TransportationAllowance { get; set; }
        
        [Display(Name = "بدل الطعام")]
        public decimal? FoodAllowance { get; set; }
        
        [Display(Name = "بدلات أخرى")]
        public decimal? OtherAllowances { get; set; }
        
        [Display(Name = "مسجل في التأمين الاجتماعي")]
        public bool SocialInsuranceEnrolled { get; set; }
        
        [StringLength(50)]
        [Display(Name = "رقم التأمين الاجتماعي")]
        public string? SocialInsuranceNumber { get; set; }
        
        [Display(Name = "نسبة التأمين الاجتماعي")]
        public decimal SocialInsurancePercentage { get; set; }
        
        [Display(Name = "مسجل في التأمين الصحي")]
        public bool HealthInsuranceEnrolled { get; set; }
        
        [Display(Name = "أيام الإجازة السنوية")]
        public int AnnualLeaveDays { get; set; }
        
        [Display(Name = "أيام الإجازة المستخدمة")]
        public int UsedLeaveDays { get; set; }
        
        [Display(Name = "أيام الإجازة المرضية")]
        public int SickLeaveDays { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اسم البنك")]
        public string? BankName { get; set; }
        
        [StringLength(50)]
        [Display(Name = "رقم الحساب البنكي")]
        public string? BankAccountNumber { get; set; }
        
        [StringLength(50)]
        [Display(Name = "رقم الآيبان")]
        public string? IBAN { get; set; }
        
        [StringLength(200)]
        [Display(Name = "اسم جهة الاتصال في حالات الطوارئ")]
        public string? EmergencyContactName { get; set; }
        
        [StringLength(20)]
        [Display(Name = "هاتف جهة الاتصال في حالات الطوارئ")]
        public string? EmergencyContactPhone { get; set; }
        
        [StringLength(100)]
        [Display(Name = "صلة القرابة")]
        public string? EmergencyContactRelation { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<EmployeeLeave> EmployeeLeaves { get; set; } = new List<EmployeeLeave>();
        public virtual ICollection<Salary> Salaries { get; set; } = new List<Salary>();
        
        // خاصية إضافية مطلوبة من FishFarmContext - لا تُخزّن في قاعدة البيانات
        [NotMapped]
        public virtual Salary? Salary { get; set; }
        
        // Helper methods
        public decimal CalculateTotalSalary()
        {
            return BasicSalary + (HousingAllowance ?? 0) + (TransportationAllowance ?? 0) + (FoodAllowance ?? 0) + (OtherAllowances ?? 0);
        }
    }
}