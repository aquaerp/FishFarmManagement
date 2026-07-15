using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;

namespace FishFarmManager.Models
{
    /// <summary>
    /// نموذج الفاتورة الضريبية
    /// Tax Invoice Model
    /// Legacy tax invoice record. ZATCA integration is governed by the G4 workflow.
    /// </summary>
    public class TaxInvoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "رقم الفاتورة")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "تاريخ الإصدار")]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Display(Name = "تاريخ التوريد")]
        public DateTime SupplyDate { get; set; }

        [Required]
        [Display(Name = "نوع الفاتورة")]
        public InvoiceType InvoiceType { get; set; } = InvoiceType.Standard;

        // بيانات البائع
        [Required]
        [StringLength(50)]
        [Display(Name = "الرقم الضريبي للبائع")]
        public string SellerVATNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "اسم البائع")]
        public string SellerName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "عنوان البائع")]
        public string? SellerAddress { get; set; }

        // بيانات المشتري
        [Required]
        public int CustomerId { get; set; }

        [StringLength(50)]
        [Display(Name = "الرقم الضريبي للمشتري")]
        public string? BuyerVATNumber { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "اسم المشتري")]
        public string BuyerName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "عنوان المشتري")]
        public string? BuyerAddress { get; set; }

        // المبالغ
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المجموع الفرعي")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الخصم")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الضريبة")]
        public decimal VATRate { get; set; } = 15m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "مبلغ الضريبة")]
        public decimal VATAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الإجمالي شامل الضريبة")]
        public decimal TotalWithVAT { get; set; }

        // QR Code
        [StringLength(1000)]
        [Display(Name = "محتوى QR Code")]
        public string? QRCodeContent { get; set; }

        [Display(Name = "QR Code Image")]
        public byte[]? QRCodeImage { get; set; }

        // ZATCA Fields
        [StringLength(100)]
        [Display(Name = "UUID")]
        public string? UUID { get; set; }

        [StringLength(500)]
        [Display(Name = "PIH (Previous Invoice Hash)")]
        public string? PIH { get; set; }

        [StringLength(500)]
        [Display(Name = "Invoice Hash")]
        public string? InvoiceHash { get; set; }

        [Display(Name = "مقدم للزكاة والضريبة")]
        public bool IsSubmittedToZATCA { get; set; }

        [Display(Name = "تاريخ التقديم للزكاة")]
        public DateTime? SubmittedToZATCADate { get; set; }

        [StringLength(100)]
        [Display(Name = "ZATCA Response Code")]
        public string? ZATCAResponseCode { get; set; }

        [StringLength(1000)]
        [Display(Name = "ZATCA Response Message")]
        public string? ZATCAResponseMessage { get; set; }

        // Reference to Sales Order
        public int? SalesOrderId { get; set; }

        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey(nameof(SalesOrderId))]
        public virtual SalesOrder? SalesOrder { get; set; }

        public virtual ICollection<TaxInvoiceItem> Items { get; set; } = new List<TaxInvoiceItem>();

        // Helper Methods
        public void CalculateTotals()
        {
            var itemsTotal = Items?.Sum(i => i.TotalAmount) ?? 0;
            SubTotal = itemsTotal;
            VATAmount = (SubTotal - DiscountAmount) * (VATRate / 100);
            TotalWithVAT = SubTotal - DiscountAmount + VATAmount;
        }

        public string GenerateQRCodeContent()
        {
            using var payload = new MemoryStream();
            WriteTlv(payload, 1, SellerName);
            WriteTlv(payload, 2, SellerVATNumber);
            WriteTlv(payload, 3, IssueDate.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture));
            WriteTlv(payload, 4, TotalWithVAT.ToString("0.00", CultureInfo.InvariantCulture));
            WriteTlv(payload, 5, VATAmount.ToString("0.00", CultureInfo.InvariantCulture));
            return Convert.ToBase64String(payload.ToArray());
        }

        private static void WriteTlv(Stream destination, byte tag, string? value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (bytes.Length > byte.MaxValue)
                throw new InvalidOperationException($"QR TLV value for tag {tag} exceeds 255 UTF-8 bytes.");
            destination.WriteByte(tag);
            destination.WriteByte((byte)bytes.Length);
            destination.Write(bytes, 0, bytes.Length);
        }
    }

    /// <summary>
    /// بند الفاتورة الضريبية
    /// Tax Invoice Item
    /// </summary>
    public class TaxInvoiceItem
    {
        public int Id { get; set; }

        [Required]
        public int TaxInvoiceId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "اسم العنصر")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "السعر")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الخصم")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الضريبة")]
        public decimal VATRate { get; set; } = 15m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "مبلغ الضريبة")]
        public decimal VATAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الإجمالي")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(TaxInvoiceId))]
        public virtual TaxInvoice TaxInvoice { get; set; } = null!;

        public void CalculateTotals()
        {
            var subtotal = Quantity * UnitPrice - DiscountAmount;
            VATAmount = subtotal * (VATRate / 100);
            TotalAmount = subtotal + VATAmount;
        }
    }

    /// <summary>
    /// نوع الفاتورة
    /// Invoice Type
    /// </summary>
    public enum InvoiceType
    {
        [Display(Name = "قياسية - Standard")]
        Standard = 1,

        [Display(Name = "مبسطة - Simplified")]
        Simplified = 2,

        [Display(Name = "ائتمان - Credit Note")]
        CreditNote = 3,

        [Display(Name = "خصم - Debit Note")]
        DebitNote = 4
    }
}


