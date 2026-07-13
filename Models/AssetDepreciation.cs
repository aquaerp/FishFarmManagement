using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج إهلاك الأصول
    /// Asset Depreciation Model
    /// </summary>
    public class AssetDepreciation
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "الأصل الثابت")]
        public int FixedAssetId { get; set; }
        
        [Required]
        [Display(Name = "السنة")]
        public int Year { get; set; }
        
        [Required]
        [Display(Name = "الشهر")]
        public int Month { get; set; }
        
        [Required]
        [Display(Name = "تاريخ الإهلاك")]
        public DateTime DepreciationDate { get; set; }
        
        [Required]
        [Display(Name = "القيمة الدفترية الافتتاحية")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBookValue { get; set; }
        
        [Required]
        [Display(Name = "مبلغ الإهلاك")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepreciationAmount { get; set; }
        
        [Required]
        [Display(Name = "الإهلاك المتراكم")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AccumulatedDepreciation { get; set; }
        
        [Required]
        [Display(Name = "القيمة الدفترية الختامية")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingBookValue { get; set; }
        
        [Display(Name = "نسبة الإهلاك المطبقة")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal AppliedRate { get; set; }
        
        [Display(Name = "عدد أيام الاستخدام")]
        public int DaysInUse { get; set; }
        
        [Display(Name = "إهلاك يومي")]
        [Column(TypeName = "decimal(18,4)")]
        public decimal DailyDepreciation { get; set; }
        
        [Display(Name = "محسوب تلقائياً")]
        public bool IsAutoCalculated { get; set; } = true;
        
        [Display(Name = "معتمد")]
        public bool IsApproved { get; set; } = false;
        
        [Display(Name = "تاريخ الاعتماد")]
        public DateTime? ApprovedDate { get; set; }
        
        [StringLength(100)]
        [Display(Name = "اعتمد بواسطة")]
        public string? ApprovedBy { get; set; }
        
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
        
        [StringLength(100)]
        [Display(Name = "حُسِب بواسطة")]
        public string? CalculatedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(100)]
        [Display(Name = "حُدّث بواسطة")]
        public string? UpdatedBy { get; set; }
        
        // Navigation Properties
        [ForeignKey(nameof(FixedAssetId))]
        public virtual FixedAsset FixedAsset { get; set; } = null!;
        
        // Computed Properties
        [NotMapped]
        public decimal DepreciationPercentage => OpeningBookValue > 0 
            ? (DepreciationAmount / OpeningBookValue) * 100 
            : 0;
        
        [NotMapped]
        public string PeriodName => $"{Year}/{Month:D2}";
        
        // Helper Methods
        public static decimal CalculateStraightLineDepreciation(decimal cost, decimal residualValue, int years)
        {
            return (cost - residualValue) / years;
        }
        
        public static decimal CalculateDecliningBalanceDepreciation(decimal bookValue, decimal rate)
        {
            return bookValue * (rate / 100);
        }
    }
}


