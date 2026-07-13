using Microsoft.EntityFrameworkCore;
using FishFarmManager.Models;
using FishFarmManager.Data;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة إدارة الفواتير الضريبية
    /// Tax Invoice Management Service
    /// </summary>
    public class TaxInvoiceService
    {
        private readonly FishFarmContext _context;
        private const decimal DEFAULT_VAT_RATE = 15.0m; // 15% VAT rate in Saudi Arabia

        public TaxInvoiceService(FishFarmContext context)
        {
            _context = context;
        }

        /// <summary>
        /// إنشاء فاتورة ضريبية تلقائياً من طلب مبيعات
        /// Create tax invoice automatically from sales order
        /// </summary>
        public async Task<TaxInvoice?> CreateTaxInvoiceFromSalesOrderAsync(int salesOrderId)
        {
            try
            {
                // Check if tax invoice already exists for this sales order
                var existingInvoice = await _context.TaxInvoices
                    .FirstOrDefaultAsync(ti => ti.SalesOrderId == salesOrderId);

                if (existingInvoice != null)
                {
                    return existingInvoice; // Already exists
                }

                // Get sales order with items and customer
                var salesOrder = await _context.SalesOrders
                    .Include(so => so.Items)
                    .Include(so => so.Customer)
                    .FirstOrDefaultAsync(so => so.Id == salesOrderId);

                if (salesOrder == null)
                    return null;

                // Get VAT configuration
                var vatConfig = await GetVATConfigurationAsync();

                // Create tax invoice
                var taxInvoice = new TaxInvoice
                {
                    InvoiceNumber = GenerateTaxInvoiceNumber(),
                    SalesOrderId = salesOrderId,
                    CustomerId = salesOrder.CustomerId,
                    IssueDate = DateTime.Now,
                    
                    // Seller information (from VAT config)
                    SellerName = vatConfig?.CompanyName ?? "شركة المزرعة السمكية",
                    SellerVATNumber = vatConfig?.TaxRegistrationNumber ?? "123456789012345",
                    SellerAddress = vatConfig?.Address ?? "الرياض، المملكة العربية السعودية",
                    
                    // Buyer information (from customer)
                    BuyerName = salesOrder.Customer.Name,
                    BuyerVATNumber = salesOrder.Customer.TaxNumber ?? "",
                    BuyerAddress = salesOrder.Customer.Address ?? "",
                    
                    // Invoice amounts
                    SubTotal = salesOrder.SubTotal,
                    DiscountAmount = salesOrder.DiscountAmount,
                    VATRate = DEFAULT_VAT_RATE,
                    VATAmount = salesOrder.VATAmount,
                    TotalWithVAT = salesOrder.TotalAmount,
                    
                    // Status (using string for now)
                    CreatedAt = DateTime.Now,
                    CreatedBy = Environment.UserName
                };

                _context.TaxInvoices.Add(taxInvoice);

                // Create tax invoice items from sales order items
                foreach (var item in salesOrder.Items)
                {
                    var taxItem = new TaxInvoiceItem
                    {
                        TaxInvoiceId = taxInvoice.Id,
                        ItemName = item.ProductName,
                        Description = $"{item.ProductName} - {item.Grade}",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.TotalPrice,
                        VATRate = DEFAULT_VAT_RATE,
                        VATAmount = item.TotalPrice * (DEFAULT_VAT_RATE / 100)
                    };

                    taxInvoice.Items.Add(taxItem);
                }

                await _context.SaveChangesAsync();
                return taxInvoice;
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking sales order creation
                System.Diagnostics.Debug.WriteLine($"Error creating tax invoice: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// إنشاء فاتورة ضريبية يدوياً
        /// Create tax invoice manually
        /// </summary>
        public async Task<TaxInvoice> CreateTaxInvoiceAsync(TaxInvoice taxInvoice)
        {
            taxInvoice.InvoiceNumber = GenerateTaxInvoiceNumber();
            taxInvoice.CreatedAt = DateTime.Now;
            taxInvoice.CalculateTotals();

            _context.TaxInvoices.Add(taxInvoice);
            await _context.SaveChangesAsync();

            return taxInvoice;
        }

        /// <summary>
        /// تحديث فاتورة ضريبية
        /// Update tax invoice
        /// </summary>
        public async Task<bool> UpdateTaxInvoiceAsync(TaxInvoice taxInvoice)
        {
            try
            {
                taxInvoice.UpdatedAt = DateTime.Now;
                taxInvoice.CalculateTotals();

                _context.TaxInvoices.Update(taxInvoice);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// الحصول على الفواتير الضريبية للعميل
        /// Get tax invoices for customer
        /// </summary>
        public async Task<List<TaxInvoice>> GetCustomerTaxInvoicesAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.TaxInvoices
                .Include(ti => ti.Items)
                .Include(ti => ti.Customer)
                .Where(ti => ti.CustomerId == customerId);

            if (fromDate.HasValue)
                query = query.Where(ti => ti.IssueDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(ti => ti.IssueDate <= toDate.Value);

            return await query.OrderByDescending(ti => ti.IssueDate).ToListAsync();
        }

        /// <summary>
        /// الحصول على جميع الفواتير الضريبية
        /// Get all tax invoices
        /// </summary>
        public async Task<List<TaxInvoice>> GetAllTaxInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.TaxInvoices
                .Include(ti => ti.Items)
                .Include(ti => ti.Customer)
                .Include(ti => ti.SalesOrder)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(ti => ti.IssueDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(ti => ti.IssueDate <= toDate.Value);

            return await query.OrderByDescending(ti => ti.IssueDate).ToListAsync();
        }

        /// <summary>
        /// الحصول على تقرير الفواتير الضريبية
        /// Get tax invoice report
        /// </summary>
        public async Task<TaxInvoiceReport> GetTaxInvoiceReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var invoices = await _context.TaxInvoices
                    .Include(ti => ti.Customer)
                    .Where(ti => ti.IssueDate >= fromDate && ti.IssueDate <= toDate)
                    .ToListAsync();

                return new TaxInvoiceReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalInvoices = invoices.Count,
                    TotalAmount = invoices.Sum(i => i.TotalWithVAT),
                    TotalVAT = invoices.Sum(i => i.VATAmount),
                    TotalSubTotal = invoices.Sum(i => i.SubTotal),
                    Invoices = invoices,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new TaxInvoiceReport 
                { 
                    FromDate = fromDate, 
                    ToDate = toDate, 
                    ErrorMessage = ex.Message 
                };
            }
        }

        /// <summary>
        /// توليد رقم فاتورة ضريبية فريد
        /// Generate unique tax invoice number
        /// </summary>
        private string GenerateTaxInvoiceNumber()
        {
            var today = DateTime.Now;
            var prefix = $"TI{today:yyyyMMdd}";
            var count = _context.TaxInvoices.Count(ti => ti.InvoiceNumber.StartsWith(prefix)) + 1;
            return $"{prefix}-{count:D4}";
        }

        /// <summary>
        /// الحصول على إعدادات ضريبة القيمة المضافة
        /// Get VAT configuration
        /// </summary>
        private async Task<VATConfiguration?> GetVATConfigurationAsync()
        {
            return await _context.VATConfigurations.FirstOrDefaultAsync();
        }
    }

    /// <summary>
    /// تقرير الفواتير الضريبية
    /// Tax Invoice Report
    /// </summary>
    public class TaxInvoiceReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalVAT { get; set; }
        public decimal TotalSubTotal { get; set; }
        public List<TaxInvoice> Invoices { get; set; } = new List<TaxInvoice>();
        public DateTime GeneratedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
