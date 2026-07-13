using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج فحص الصحة - يحتوي على جميع المعلومات المطلوبة لفحص صحة الأسماك
    /// Health Inspection Model - Contains all required information for fish health inspection
    /// </summary>
    public class HealthInspection
    {
        public int Id { get; set; }
        
        [Required]
        public int PondId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الفحص")]
        public string InspectionNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime InspectionDate { get; set; } = DateTime.Now;
        
        [Display(Name = "دورة الإنتاج")]
        public int? ProductionCycleId { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "اسم المفتش")]
        public string InspectorName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "نوع الفحص")]
        public HealthInspectionType InspectionType { get; set; }
        
        [Required]
        [Display(Name = "حجم العينة")]
        public int SampleSize { get; set; }
        
        [Required]
        [Display(Name = "نسبة العينة")]
        public decimal SamplePercentage { get; set; }
        
        [Required]
        [Display(Name = "عدد الأسماك السليمة")]
        public int HealthyCount { get; set; }
        
        [Required]
        [Display(Name = "عدد الأسماك المريضة")]
        public int SickCount { get; set; }
        
        [Required]
        [Display(Name = "عدد الأسماك النافقة")]
        public int DeadCount { get; set; }
        
        [Required]
        [Display(Name = "درجة الحالة الجسدية")]
        public decimal BodyConditionScore { get; set; }
        
        [Required]
        [Display(Name = "آفات جلدية")]
        public bool HasSkinLesions { get; set; }
        
        [Required]
        [Display(Name = "تلف الزعانف")]
        public bool HasFinDamage { get; set; }
        
        [Required]
        [Display(Name = "مشاكل العين")]
        public bool HasEyeProblems { get; set; }
        
        [Required]
        [Display(Name = "مشاكل الخياشيم")]
        public bool HasGillProblems { get; set; }
        
        [Required]
        [Display(Name = "انتفاخ")]
        public bool HasBloating { get; set; }
        
        [Required]
        [Display(Name = "تغير اللون")]
        public bool HasDiscoloration { get; set; }
        
        [Required]
        [Display(Name = "السباحة الطبيعية")]
        public bool NormalSwimming { get; set; }
        
        [Required]
        [Display(Name = "التغذية الطبيعية")]
        public bool NormalFeeding { get; set; }
        
        [Required]
        [Display(Name = "الخمول")]
        public bool Lethargy { get; set; }
        
        [Required]
        [Display(Name = "التجمع غير الطبيعي")]
        public bool AbnormalGathering { get; set; }
        
        [Required]
        [Display(Name = "اللهاث على السطح")]
        public bool SurfaceGasping { get; set; }
        
        [Required]
        [Display(Name = "كشف الطفيليات")]
        public bool ParasiteDetection { get; set; }
        
        [StringLength(500)]
        [Display(Name = "أنواع الطفيليات")]
        public string? ParasiteTypes { get; set; }
        
        [Required]
        [Display(Name = "العدوى البكتيرية")]
        public bool BacterialInfection { get; set; }
        
        [StringLength(500)]
        [Display(Name = "أنواع البكتيريا")]
        public string? BacteriaTypes { get; set; }
        
        [Required]
        [Display(Name = "العدوى الفيروسية")]
        public bool ViralInfection { get; set; }
        
        [StringLength(500)]
        [Display(Name = "أنواع الفيروسات")]
        public string? VirusTypes { get; set; }
        
        [Required]
        [Display(Name = "العدوى الفطرية")]
        public bool FungalInfection { get; set; }
        
        [StringLength(500)]
        [Display(Name = "أنواع الفطريات")]
        public string? FungusTypes { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "التشخيص الأساسي")]
        public string PrimaryDiagnosis { get; set; } = string.Empty;
        
        [StringLength(200)]
        [Display(Name = "التشخيص الثانوي")]
        public string? SecondaryDiagnosis { get; set; }
        
        [Required]
        [Display(Name = "الحالة الصحية العامة")]
        public HealthStatus OverallHealthStatus { get; set; }
        
        [Required]
        [Display(Name = "معدل النفوق")]
        public decimal MortalityRate { get; set; }
        
        [Required]
        [Display(Name = "يتطلب علاج")]
        public bool TreatmentRequired { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "العلاج الموصى به")]
        public string? RecommendedTreatment { get; set; }
        
        [Required]
        [Display(Name = "يتطلب عزل")]
        public bool IsolationRequired { get; set; }
        
        [Required]
        [Display(Name = "يتطلب إعدام")]
        public bool CullingRequired { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "الإجراءات الوقائية")]
        public string? PreventiveMeasures { get; set; }
        
        [Display(Name = "تاريخ المتابعة")]
        public DateTime? FollowUpDate { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "ملاحظات المتابعة")]
        public string? FollowUpNotes { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اسم الطبيب البيطري")]
        public string? VeterinarianName { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اسم المختبر")]
        public string? LaboratoryName { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "الملاحظات العامة")]
        public string? GeneralObservations { get; set; }
        
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