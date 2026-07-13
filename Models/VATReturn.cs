using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// إقرار ضريبة القيمة المضافة
    /// VAT Return Model - Saudi Arabia
    /// </summary>
    [Table("VATReturns")]
    public class VATReturn
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// رقم فترة الإقرار
        /// </summary>
        [Required]
        [StringLength(20)]
        public string PeriodNumber { get; set; } = string.Empty;

        /// <summary>
        /// بداية فترة الإقرار
        /// </summary>
        [Required]
        public DateTime PeriodStartDate { get; set; }

        /// <summary>
        /// نهاية فترة الإقرار
        /// </summary>
        [Required]
        public DateTime PeriodEndDate { get; set; }

        /// <summary>
        /// تاريخ الاستحقاق
        /// </summary>
        [Required]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// تاريخ التقديم
        /// </summary>
        public DateTime? SubmissionDate { get; set; }

        /// <summary>
        /// حالة الإقرار
        /// </summary>
        [Required]
        public VATReturnStatus Status { get; set; } = VATReturnStatus.Draft;

        /// <summary>
        /// رقم الهوية الضريبية
        /// </summary>
        [Required]
        [StringLength(15)]
        public string TaxRegistrationNumber { get; set; } = string.Empty;

        // مبيعات خاضعة للضريبة
        /// <summary>
        /// المبيعات المحلية الخاضعة لضريبة القيمة المضافة (الصندوق 1)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box1_TaxableSalesInKSA { get; set; }

        /// <summary>
        /// مبيعات الصفر (الصندوق 2)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box2_ZeroRatedSales { get; set; }

        /// <summary>
        /// الصادرات (الصندوق 3)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box3_Exports { get; set; }

        /// <summary>
        /// مبيعات معفاة (الصندوق 4)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box4_ExemptSales { get; set; }

        /// <summary>
        /// إجمالي المبيعات (الصندوق 5)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box5_TotalSales { get; set; }

        /// <summary>
        /// ضريبة القيمة المضافة على المبيعات (الصندوق 6)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box6_VATOnSales { get; set; }

        // المشتريات والضريبة المستردة
        /// <summary>
        /// إجمالي المشتريات (الصندوق 7)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box7_TotalPurchases { get; set; }

        /// <summary>
        /// مشتريات من دول مجلس التعاون (الصندوق 8)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box8_GCCPurchases { get; set; }

        /// <summary>
        /// الواردات الخاضعة للضريبة (الصندوق 9)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box9_TaxableImports { get; set; }

        /// <summary>
        /// ضريبة القيمة المضافة على المشتريات (الصندوق 10)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box10_VATOnPurchases { get; set; }

        /// <summary>
        /// صافي ضريبة القيمة المضافة المستحقة (الصندوق 11)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box11_NetVATDue { get; set; }

        /// <summary>
        /// تعديلات (الصندوق 12)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box12_Adjustments { get; set; }

        /// <summary>
        /// إجمالي ضريبة القيمة المضافة المستحقة (الصندوق 13)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box13_TotalVATDue { get; set; }

        /// <summary>
        /// المبلغ المسترد من الفترة السابقة (الصندوق 14)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box14_RecoverablePreviousPeriod { get; set; }

        /// <summary>
        /// صافي ضريبة القيمة المضافة المستحقة للفترة (الصندوق 15)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Box15_NetVATDueForPeriod { get; set; }

        // بيانات إضافية مطلوبة في السعودية
        /// <summary>
        /// ملاحظات
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// تم إنشاؤه بواسطة
        /// </summary>
        [Required]
        public int CreatedById { get; set; }

        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// تم التعديل بواسطة
        /// </summary>
        public int? ModifiedById { get; set; }

        /// <summary>
        /// تاريخ التعديل
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// اسم الملف المرفق
        /// </summary>
        [StringLength(255)]
        public string? AttachmentFileName { get; set; }

        /// <summary>
        /// حساب الضريبة تلقائياً
        /// </summary>
        public void CalculateVATAmounts()
        {
            // حساب إجمالي المبيعات (صندوق 5)
            Box5_TotalSales = Box1_TaxableSalesInKSA + Box2_ZeroRatedSales + Box3_Exports + Box4_ExemptSales;

            // حساب ضريبة القيمة المضافة على المبيعات (صندوق 6)
            Box6_VATOnSales = Box1_TaxableSalesInKSA * 0.15m; // معدل 15%

            // حساب صافي ضريبة القيمة المضافة المستحقة (صندوق 11)
            Box11_NetVATDue = Box6_VATOnSales - Box10_VATOnPurchases;

            // حساب إجمالي ضريبة القيمة المضافة المستحقة (صندوق 13)
            Box13_TotalVATDue = Box11_NetVATDue + Box12_Adjustments;

            // حساب صافي ضريبة القيمة المضافة المستحقة للفترة (صندوق 15)
            Box15_NetVATDueForPeriod = Box13_TotalVATDue - Box14_RecoverablePreviousPeriod;
        }

        /// <summary>
        /// تحديد موعد الاستحقاق بناءً على نهاية الفترة
        /// </summary>
        public void SetDueDate()
        {
            // في السعودية، موعد التقديم خلال 30 يوم من نهاية الفترة الضريبية
            DueDate = PeriodEndDate.AddDays(30);
        }

        /// <summary>
        /// تحديد رقم الفترة تلقائياً
        /// </summary>
        public void GeneratePeriodNumber()
        {
            PeriodNumber = $"{PeriodStartDate:yyyyMM}";
        }

        // Navigation Properties
        public virtual User? CreatedBy { get; set; }
        public virtual User? ModifiedBy { get; set; }
    }

    /// <summary>
    /// حالة إقرار ضريبة القيمة المضافة
    /// </summary>
    public enum VATReturnStatus
    {
        [Display(Name = "مسودة")]
        Draft = 1,

        [Display(Name = "مراجعة")]
        UnderReview = 2,

        [Display(Name = "معتمد")]
        Approved = 3,

        [Display(Name = "مقدم")]
        Submitted = 4,

        [Display(Name = "مدفوع")]
        Paid = 5,

        [Display(Name = "مغلق")]
        Closed = 6,

        [Display(Name = "ملغي")]
        Cancelled = 7
    }
}