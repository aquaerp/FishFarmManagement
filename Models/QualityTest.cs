using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج اختبار الجودة - يحتوي على جميع المعلومات المطلوبة لاختبارات الجودة
    /// Quality Test Model - Contains all required information for quality testing
    /// </summary>
    public class QualityTest
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الاختبار")]
        public string TestNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime TestDate { get; set; } = DateTime.Now;
        
        [Display(Name = "دورة الإنتاج")]
        public int? ProductionCycleId { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "نوع الاختبار")]
        public string TestType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم الاختبار")]
        public string TestName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "نوع العينة")]
        public QualitySampleType SampleType { get; set; }
        
        [Required]
        [Display(Name = "حجم العينة")]
        public int SampleSize { get; set; }
        
        [StringLength(200)]
        [Display(Name = "موقع العينة")]
        public string? SampleLocation { get; set; }
        
        [Required]
        [Display(Name = "النتيجة")]
        public decimal Result { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string Unit { get; set; } = string.Empty;
        
        [Display(Name = "متوسط الوزن")]
        public decimal? AverageWeight { get; set; }
        
        [Display(Name = "متوسط الطول")]
        public decimal? AverageLength { get; set; }
        
        [Display(Name = "نسبة التوحيد")]
        public decimal? UniformityPercentage { get; set; }
        
        [StringLength(200)]
        [Display(Name = "المظهر")]
        public string? Appearance { get; set; }
        
        [Display(Name = "الرطوبة")]
        public decimal? Moisture { get; set; }
        
        [Display(Name = "البروتين")]
        public decimal? Protein { get; set; }
        
        [Display(Name = "الدهون")]
        public decimal? Fat { get; set; }
        
        [Display(Name = "الرماد")]
        public decimal? Ash { get; set; }
        
        [Display(Name = "درجة الحموضة")]
        public decimal? pH { get; set; }
        
        [Display(Name = "إجمالي البكتيريا")]
        public decimal? TotalBacteriaCount { get; set; }
        
        [Display(Name = "عدد القولونيات")]
        public decimal? ColiformCount { get; set; }
        
        [Display(Name = "وجود السالمونيلا")]
        public bool? SalmonellaPresence { get; set; }
        
        [Display(Name = "وجود الإيشيريشيا كولاي")]
        public bool? EColiPresence { get; set; }
        
        [Display(Name = "درجة اللون")]
        public decimal? ColorScore { get; set; }
        
        [Display(Name = "درجة الرائحة")]
        public decimal? OdorScore { get; set; }
        
        [Display(Name = "درجة الملمس")]
        public decimal? TextureScore { get; set; }
        
        [Display(Name = "درجة الطعم")]
        public decimal? TasteScore { get; set; }
        
        [Display(Name = "الزئبق")]
        public decimal? Mercury { get; set; }
        
        [Display(Name = "الرصاص")]
        public decimal? Lead { get; set; }
        
        [Display(Name = "الكادميوم")]
        public decimal? Cadmium { get; set; }
        
        [Display(Name = "الزرنيخ")]
        public decimal? Arsenic { get; set; }
        
        [Required]
        [Display(Name = "نتيجة الاختبار")]
        public QualityTestResult TestResult { get; set; }
        
        [Display(Name = "الدرجة الإجمالية")]
        public decimal? OverallScore { get; set; }
        
        [Display(Name = "يلبي المعايير")]
        public bool? MeetsStandards { get; set; }
        
        [StringLength(200)]
        [Display(Name = "مرجع المعايير")]
        public string? StandardsReference { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اختبر بواسطة")]
        public string? TestedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "وافق بواسطة")]
        public string? ApprovedBy { get; set; }
        
        [StringLength(100)]
        [Display(Name = "المختبر")]
        public string? Laboratory { get; set; }
        
        [StringLength(100)]
        [Display(Name = "رقم الشهادة")]
        public string? CertificateNumber { get; set; }
        
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
        
        // Navigation properties
        public virtual Pond Pond { get; set; } = null!;
        public virtual ProductionCycle? ProductionCycle { get; set; }
    }
}