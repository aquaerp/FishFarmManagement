using System;
using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج المستخدم - User Model
    /// يمثل مستخدمي النظام مع معلومات المصادقة والصلاحيات
    /// </summary>
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(50)]
        public string? UpdatedBy { get; set; }

        // Navigation property - ربط مع الموظف إن وجد
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }
    }

    /// <summary>
    /// أدوار المستخدمين - User Roles
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// مدير النظام - كامل الصلاحيات
        /// </summary>
        Admin = 1,

        /// <summary>
        /// مدير - صلاحيات إدارية واسعة
        /// </summary>
        Manager = 2,

        /// <summary>
        /// محاسب - الوصول للتقارير المالية
        /// </summary>
        Accountant = 3,

        /// <summary>
        /// موظف إنتاج - تسجيل البيانات الإنتاجية
        /// </summary>
        ProductionStaff = 4,

        /// <summary>
        /// موظف مبيعات - إدارة المبيعات والعملاء
        /// </summary>
        SalesStaff = 5,

        /// <summary>
        /// موظف مخزون - إدارة المخزون
        /// </summary>
        InventoryStaff = 6,

        /// <summary>
        /// مشرف جودة - مراقبة الجودة والصحة
        /// </summary>
        QualityControl = 7,

        /// <summary>
        /// موظف صيانة - إدارة الصيانة والمعدات
        /// </summary>
        MaintenanceStaff = 8,

        /// <summary>
        /// موظف موارد بشرية - إدارة الموظفين والرواتب
        /// </summary>
        HRStaff = 9,

        /// <summary>
        /// مشاهد فقط - قراءة التقارير دون تعديل
        /// </summary>
        Viewer = 10
    }
}
