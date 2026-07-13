using Microsoft.EntityFrameworkCore;
using FishFarmManager.Models;
using FishFarmManager.Data;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة حساب رصيد العملاء
    /// Customer Balance Calculation Service
    /// </summary>
    public class CustomerBalanceService
    {
        private readonly FishFarmContext _context;

        public CustomerBalanceService(FishFarmContext context)
        {
            _context = context;
        }

        /// <summary>
        /// حساب الرصيد الحقيقي للعميل من جميع المعاملات
        /// Calculate real customer balance from all transactions
        /// </summary>
        public async Task<CustomerBalanceInfo> CalculateCustomerBalanceAsync(int customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                    return new CustomerBalanceInfo { CustomerId = customerId, IsValid = false };

                // Get all sales orders for this customer
                var salesOrders = await _context.SalesOrders
                    .Where(so => so.CustomerId == customerId)
                    .ToListAsync();

                // Get all customer payments
                var payments = await _context.CustomerPayments
                    .Where(cp => cp.CustomerId == customerId)
                    .ToListAsync();

                // Calculate total sales
                var totalSales = salesOrders.Sum(so => so.TotalAmount);

                // Calculate total payments
                var totalPayments = payments.Sum(p => p.Amount);

                // Calculate balance
                var calculatedBalance = totalSales - totalPayments;

                return new CustomerBalanceInfo
                {
                    CustomerId = customerId,
                    CustomerName = customer.Name,
                    StoredBalance = customer.CurrentBalance,
                    CalculatedBalance = calculatedBalance,
                    TotalSales = totalSales,
                    TotalPayments = totalPayments,
                    IsValid = Math.Abs(customer.CurrentBalance - calculatedBalance) < 0.01m,
                    LastCalculated = DateTime.Now,
                    SalesOrderCount = salesOrders.Count,
                    PaymentCount = payments.Count
                };
            }
            catch (Exception ex)
            {
                return new CustomerBalanceInfo 
                { 
                    CustomerId = customerId, 
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// تحديث الرصيد المخزن للعميل بناءً على الحساب الفعلي
        /// Update stored customer balance based on actual calculation
        /// </summary>
        public async Task<bool> UpdateCustomerBalanceAsync(int customerId)
        {
            try
            {
                var balanceInfo = await CalculateCustomerBalanceAsync(customerId);
                if (!balanceInfo.IsValid)
                    return false;

                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null)
                    return false;

                customer.CurrentBalance = balanceInfo.CalculatedBalance;
                customer.LastTransactionDate = DateTime.Now;
                customer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// تحديث رصيد جميع العملاء
        /// Update balance for all customers
        /// </summary>
        public async Task<BalanceUpdateResult> UpdateAllCustomerBalancesAsync()
        {
            var result = new BalanceUpdateResult();
            
            try
            {
                var customers = await _context.Customers.ToListAsync();
                
                foreach (var customer in customers)
                {
                    var balanceInfo = await CalculateCustomerBalanceAsync(customer.Id);
                    
                    if (balanceInfo.IsValid && 
                        Math.Abs(customer.CurrentBalance - balanceInfo.CalculatedBalance) > 0.01m)
                    {
                        customer.CurrentBalance = balanceInfo.CalculatedBalance;
                        customer.UpdatedAt = DateTime.Now;
                        result.UpdatedCount++;
                    }
                    else if (!balanceInfo.IsValid)
                    {
                        result.Errors.Add($"خطأ في حساب رصيد العميل {customer.Name}: {balanceInfo.ErrorMessage}");
                    }
                }

                if (result.UpdatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"خطأ عام: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// الحصول على تقرير رصيد العميل
        /// Get customer balance report
        /// </summary>
        public async Task<CustomerBalanceReport> GetCustomerBalanceReportAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var from = fromDate ?? DateTime.Now.AddMonths(-12);
                var to = toDate ?? DateTime.Now;

                var salesOrders = await _context.SalesOrders
                    .Where(so => so.CustomerId == customerId && 
                               so.OrderDate >= from && so.OrderDate <= to)
                    .OrderBy(so => so.OrderDate)
                    .ToListAsync();

                var payments = await _context.CustomerPayments
                    .Where(cp => cp.CustomerId == customerId && 
                               cp.PaymentDate >= from && cp.PaymentDate <= to)
                    .OrderBy(cp => cp.PaymentDate)
                    .ToListAsync();

                var balanceInfo = await CalculateCustomerBalanceAsync(customerId);

                return new CustomerBalanceReport
                {
                    CustomerId = customerId,
                    CustomerName = balanceInfo.CustomerName,
                    FromDate = from,
                    ToDate = to,
                    OpeningBalance = await GetOpeningBalanceAsync(customerId, from),
                    TotalSales = salesOrders.Sum(so => so.TotalAmount),
                    TotalPayments = payments.Sum(p => p.Amount),
                    ClosingBalance = balanceInfo.CalculatedBalance,
                    SalesOrders = salesOrders,
                    Payments = payments,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new CustomerBalanceReport 
                { 
                    CustomerId = customerId, 
                    ErrorMessage = ex.Message 
                };
            }
        }

        private async Task<decimal> GetOpeningBalanceAsync(int customerId, DateTime fromDate)
        {
            var salesOrders = await _context.SalesOrders
                .Where(so => so.CustomerId == customerId && so.OrderDate < fromDate)
                .ToListAsync();

            var payments = await _context.CustomerPayments
                .Where(cp => cp.CustomerId == customerId && cp.PaymentDate < fromDate)
                .ToListAsync();

            return salesOrders.Sum(so => so.TotalAmount) - payments.Sum(p => p.Amount);
        }
    }

    /// <summary>
    /// معلومات رصيد العميل
    /// Customer Balance Information
    /// </summary>
    public class CustomerBalanceInfo
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal StoredBalance { get; set; }
        public decimal CalculatedBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPayments { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LastCalculated { get; set; }
        public int SalesOrderCount { get; set; }
        public int PaymentCount { get; set; }

        public decimal BalanceDifference => CalculatedBalance - StoredBalance;
    }

    /// <summary>
    /// نتيجة تحديث الرصيد
    /// Balance Update Result
    /// </summary>
    public class BalanceUpdateResult
    {
        public bool Success { get; set; }
        public int UpdatedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public string GetSummary()
        {
            if (Success)
                return $"تم تحديث رصيد {UpdatedCount} عميل بنجاح";
            else
                return $"فشل في التحديث: {string.Join(", ", Errors)}";
        }
    }

    /// <summary>
    /// تقرير رصيد العميل
    /// Customer Balance Report
    /// </summary>
    public class CustomerBalanceReport
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
        public List<CustomerPayment> Payments { get; set; } = new List<CustomerPayment>();
        public DateTime GeneratedAt { get; set; }
        public string? ErrorMessage { get; set; }

        public decimal NetMovement => TotalSales - TotalPayments;
    }
}
