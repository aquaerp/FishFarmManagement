using System;
using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// إعدادات ضريبة القيمة المضافة
    /// VAT Configuration Model
    /// </summary>
    public class VATConfiguration
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "الرقم الضريبي")]
        public string TaxRegistrationNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "اسم المنشأة")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "اسم المنشأة بالإنجليزية")]
        public string? CompanyNameEN { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "العنوان")]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "الرمز البريدي")]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        [Display(Name = "المدينة")]
        public string? City { get; set; }

        [StringLength(100)]
        [Display(Name = "المنطقة")]
        public string? Region { get; set; }

        [StringLength(50)]
        [Display(Name = "الدولة")]
        public string Country { get; set; } = "المملكة العربية السعودية";

        [Required]
        [Display(Name = "نسبة الضريبة الافتراضية")]
        public decimal DefaultVATRate { get; set; } = 15m;

        [Display(Name = "تفعيل الفوترة الإلكترونية")]
        public bool EnableEInvoicing { get; set; } = true;

        [Display(Name = "تفعيل ZATCA Integration")]
        public bool EnableZATCAIntegration { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "ZATCA API Endpoint")]
        public string? ZATCAApiEndpoint { get; set; }

        [StringLength(200)]
        [Display(Name = "ZATCA API Key")]
        public string? ZATCAApiKey { get; set; }

        [StringLength(100)]
        [Display(Name = "رقم الجهاز (Device ID)")]
        public string? DeviceId { get; set; }

        [Display(Name = "تاريخ التسجيل في الضريبة")]
        public DateTime VATRegistrationDate { get; set; }

        [Display(Name = "فترة الإقرار")]
        public VATReturnPeriod ReturnPeriod { get; set; } = VATReturnPeriod.Monthly;

        [Display(Name = "يوم تقديم الإقرار")]
        public int ReturnSubmissionDay { get; set; } = 28;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// فترة الإقرار الضريبي
    /// VAT Return Period
    /// </summary>
    public enum VATReturnPeriod
    {
        [Display(Name = "شهري")]
        Monthly = 1,

        [Display(Name = "ربع سنوي")]
        Quarterly = 2,

        [Display(Name = "نصف سنوي")]
        SemiAnnual = 3,

        [Display(Name = "سنوي")]
        Annual = 4
    }
}


