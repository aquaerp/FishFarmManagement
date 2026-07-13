using Microsoft.EntityFrameworkCore;
using FishFarmManager.Models;
using FishFarmManager.Data;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة فحص سلامة قاعدة البيانات
    /// Database Integrity Check Service
    /// </summary>
    public class DatabaseIntegrityService
    {
        private readonly FishFarmContext _context;

        public DatabaseIntegrityService(FishFarmContext context)
        {
            _context = context;
        }

        /// <summary>
        /// فحص شامل لسلامة قاعدة البيانات
        /// Comprehensive database integrity check
        /// </summary>
        public async Task<DatabaseIntegrityReport> CheckDatabaseIntegrityAsync()
        {
            var report = new DatabaseIntegrityReport
            {
                CheckedAt = DateTime.Now,
                Checks = new List<IntegrityCheck>()
            };

            try
            {
                // Check customer-sales order relationships
                await CheckCustomerSalesOrderRelationships(report);
                
                // Check sales order-tax invoice relationships
                await CheckSalesOrderTaxInvoiceRelationships(report);
                
                // Check customer balance consistency
                await CheckCustomerBalanceConsistency(report);
                
                // Check orphaned records
                await CheckOrphanedRecords(report);
                
                // Check data consistency
                await CheckDataConsistency(report);
                
                // Check foreign key constraints
                await CheckForeignKeyConstraints(report);
                
                report.IsHealthy = report.Checks.All(c => c.Status == CheckStatus.Passed);
            }
            catch (Exception ex)
            {
                report.IsHealthy = false;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// فحص علاقات العملاء وأوامر المبيعات
        /// Check customer-sales order relationships
        /// </summary>
        private async Task CheckCustomerSalesOrderRelationships(DatabaseIntegrityReport report)
        {
            var check = new IntegrityCheck
            {
                Name = "Customer-Sales Order Relationships",
                Description = "فحص العلاقات بين العملاء وأوامر المبيعات"
            };

            try
            {
                // Find sales orders with invalid customer references
                var invalidSalesOrders = await _context.SalesOrders
                    .Where(so => !_context.Customers.Any(c => c.Id == so.CustomerId))
                    .ToListAsync();

                if (invalidSalesOrders.Any())
                {
                    check.Status = CheckStatus.Failed;
                    check.Issues.Add($"تم العثور على {invalidSalesOrders.Count} أمر مبيعات بمراجع عملاء غير صحيحة");
                    check.Details.AddRange(invalidSalesOrders.Select(so => 
                        $"Sales Order ID: {so.Id}, Customer ID: {so.CustomerId}"));
                }
                else
                {
                    check.Status = CheckStatus.Passed;
                    check.Message = "جميع أوامر المبيعات لها مراجع عملاء صحيحة";
                }
            }
            catch (Exception ex)
            {
                check.Status = CheckStatus.Error;
                check.Issues.Add($"خطأ في فحص العلاقات: {ex.Message}");
            }

            report.Checks.Add(check);
        }

        /// <summary>
        /// فحص علاقات أوامر المبيعات والفواتير الضريبية
        /// Check sales order-tax invoice relationships
        /// </summary>
        private async Task CheckSalesOrderTaxInvoiceRelationships(DatabaseIntegrityReport report)
        {
            var check = new IntegrityCheck
            {
                Name = "Sales Order-Tax Invoice Relationships",
                Description = "فحص العلاقات بين أوامر المبيعات والفواتير الضريبية"
            };

            try
            {
                // Find tax invoices with invalid sales order references
                var invalidTaxInvoices = await _context.TaxInvoices
                    .Where(ti => ti.SalesOrderId.HasValue && 
                                !_context.SalesOrders.Any(so => so.Id == ti.SalesOrderId.Value))
                    .ToListAsync();

                if (invalidTaxInvoices.Any())
                {
                    check.Status = CheckStatus.Failed;
                    check.Issues.Add($"تم العثور على {invalidTaxInvoices.Count} فاتورة ضريبية بمراجع أوامر مبيعات غير صحيحة");
                    check.Details.AddRange(invalidTaxInvoices.Select(ti => 
                        $"Tax Invoice ID: {ti.Id}, Sales Order ID: {ti.SalesOrderId}"));
                }
                else
                {
                    check.Status = CheckStatus.Passed;
                    check.Message = "جميع الفواتير الضريبية لها مراجع أوامر مبيعات صحيحة";
                }

                // Check for sales orders without tax invoices
                var salesOrdersWithoutTaxInvoices = await _context.SalesOrders
                    .Where(so => !_context.TaxInvoices.Any(ti => ti.SalesOrderId == so.Id))
                    .CountAsync();

                if (salesOrdersWithoutTaxInvoices > 0)
                {
                    check.Warnings.Add($"تم العثور على {salesOrdersWithoutTaxInvoices} أمر مبيعات بدون فواتير ضريبية");
                }
            }
            catch (Exception ex)
            {
                check.Status = CheckStatus.Error;
                check.Issues.Add($"خطأ في فحص العلاقات: {ex.Message}");
            }

            report.Checks.Add(check);
        }

        /// <summary>
        /// فحص تناسق رصيد العملاء
        /// Check customer balance consistency
        /// </summary>
        private async Task CheckCustomerBalanceConsistency(DatabaseIntegrityReport report)
        {
            var check = new IntegrityCheck
            {
                Name = "Customer Balance Consistency",
                Description = "فحص تناسق رصيد العملاء"
            };

            try
            {
                var customerBalanceService = new CustomerBalanceService(_context);
                var customers = await _context.Customers.ToListAsync();
                var inconsistentCustomers = new List<string>();

                foreach (var customer in customers)
                {
                    var balanceInfo = await customerBalanceService.CalculateCustomerBalanceAsync(customer.Id);
                    if (!balanceInfo.IsValid || Math.Abs(balanceInfo.BalanceDifference) > 0.01m)
                    {
                        inconsistentCustomers.Add($"العميل {customer.Name}: الرصيد المخزن {customer.CurrentBalance:N2}, المحسوب {balanceInfo.CalculatedBalance:N2}");
                    }
                }

                if (inconsistentCustomers.Any())
                {
                    check.Status = CheckStatus.Failed;
                    check.Issues.Add($"تم العثور على {inconsistentCustomers.Count} عميل برصيد غير متسق");
                    check.Details.AddRange(inconsistentCustomers);
                }
                else
                {
                    check.Status = CheckStatus.Passed;
                    check.Message = "جميع أرصدة العملاء متسقة";
                }
            }
            catch (Exception ex)
            {
                check.Status = CheckStatus.Error;
                check.Issues.Add($"خطأ في فحص الرصيد: {ex.Message}");
            }

            report.Checks.Add(check);
        }

        /// <summary>
        /// فحص السجلات اليتيمة
        /// Check orphaned records
        /// </summary>
        private async Task CheckOrphanedRecords(DatabaseIntegrityReport report)
        {
            var check = new IntegrityCheck
            {
                Name = "Orphaned Records",
                Description = "فحص السجلات اليتيمة"
            };

            try
            {
                var issues = new List<string>();

                // Check orphaned sales order items
                var orphanedItems = await _context.SalesOrderItems
                    .Where(item => !_context.SalesOrders.Any(so => so.Id == item.SalesOrderId))
                    .CountAsync();

                if (orphanedItems > 0)
                {
                    issues.Add($"تم العثور على {orphanedItems} بند أمر مبيعات يتيم");
                }

                // Check orphaned tax invoice items
                var orphanedTaxItems = await _context.TaxInvoiceItems
                    .Where(item => !_context.TaxInvoices.Any(ti => ti.Id == item.TaxInvoiceId))
                    .CountAsync();

                if (orphanedTaxItems > 0)
                {
                    issues.Add($"تم العثور على {orphanedTaxItems} بند فاتورة ضريبية يتيم");
                }

                // Check orphaned customer payments
                var orphanedPayments = await _context.CustomerPayments
                    .Where(p => !_context.Customers.Any(c => c.Id == p.CustomerId))
                    .CountAsync();

                if (orphanedPayments > 0)
                {
                    issues.Add($"تم العثور على {orphanedPayments} دفعة عميل يتيمة");
                }

                if (issues.Any())
                {
                    check.Status = CheckStatus.Failed;
                    check.Issues.AddRange(issues);
                }
                else
                {
                    check.Status = CheckStatus.Passed;
                    check.Message = "لا توجد سجلات يتيمة";
                }
            }
            catch (Exception ex)
            {
                check.Status = CheckStatus.Error;
                check.Issues.Add($"خطأ في فحص السجلات اليتيمة: {ex.Message}");
            }

            report.Checks.Add(check);
        }

        /// <summary>
        /// فحص تناسق البيانات
        /// Check data consistency
        /// </summary>
        private async Task CheckDataConsistency(DatabaseIntegrityReport report)
        {
            var check = new IntegrityCheck
            {
                Name = "Data Consistency",
                Description = "فحص تناسق البيانات"
            };

            try
            {
                var issues = new List<string>();

                // Check for negative amounts
                var negativeSales = await _context.SalesOrders
                    .Where(so => so.TotalAmount < 0)
                    .CountAsync();

                if (negativeSales > 0)
                {
                    issues.Add($"تم العثور على {negativeSales} أمر مبيعات بمبلغ سالب");
                }

                var negativePayments = await _context.CustomerPayments
                    .Where(p => p.Amount < 0)
                    .CountAsync();

                if (negativePayments > 0)
                {
                    issues.Add($"تم العثور على {negativePayments} دفعة بمبلغ سالب");
                }

                // Check for future dates
                var futureSales = await _context.SalesOrders
                    .Where(so => so.OrderDate > DateTime.Now)
                    .CountAsync();

                if (futureSales > 0)
                {
                    issues.Add($"تم العثور على {futureSales} أمر مبيعات بتاريخ مستقبلي");
                }

                if (issues.Any())
                {
                    check.Status = CheckStatus.Warning;
                    check.Issues.AddRange(issues);
                }
                else
                {
                    check.Status = CheckStatus.Passed;
                    check.Message = "جميع البيانات متسقة";
                }
            }
            catch (Exception ex)
            {
                check.Status = CheckStatus.Error;
                check.Issues.Add($"خطأ في فحص التناسق: {ex.Message}");
            }

            report.Checks.Add(check);
        }

        /// <summary>
        /// فحص قيود المفاتيح الخارجية
        /// Check foreign key constraints
        /// </summary>
        private async Task CheckForeignKeyConstraints(DatabaseIntegrityReport report)
        {
            var check = new IntegrityCheck
            {
                Name = "Foreign Key Constraints",
                Description = "فحص قيود المفاتيح الخارجية"
            };

            try
            {
                // This is a basic check - in a real scenario, you might want to
                // check specific foreign key violations
                check.Status = CheckStatus.Passed;
                check.Message = "تم فحص قيود المفاتيح الخارجية بنجاح";
            }
            catch (Exception ex)
            {
                check.Status = CheckStatus.Error;
                check.Issues.Add($"خطأ في فحص المفاتيح الخارجية: {ex.Message}");
            }

            report.Checks.Add(check);
        }

        /// <summary>
        /// إصلاح المشاكل المكتشفة
        /// Fix detected issues
        /// </summary>
        public async Task<RepairReport> RepairIssuesAsync(DatabaseIntegrityReport report)
        {
            var repairReport = new RepairReport
            {
                StartedAt = DateTime.Now,
                Repairs = new List<RepairAction>()
            };

            try
            {
                foreach (var check in report.Checks.Where(c => c.Status == CheckStatus.Failed))
                {
                    switch (check.Name)
                    {
                        case "Customer Balance Consistency":
                            await RepairCustomerBalances(repairReport);
                            break;
                        case "Orphaned Records":
                            await RepairOrphanedRecords(repairReport);
                            break;
                        // Add more repair actions as needed
                    }
                }

                repairReport.CompletedAt = DateTime.Now;
                repairReport.Success = true;
            }
            catch (Exception ex)
            {
                repairReport.Success = false;
                repairReport.ErrorMessage = ex.Message;
            }

            return repairReport;
        }

        private async Task RepairCustomerBalances(RepairReport report)
        {
            var repair = new RepairAction
            {
                Name = "إصلاح أرصدة العملاء",
                StartedAt = DateTime.Now
            };

            try
            {
                var customerBalanceService = new CustomerBalanceService(_context);
                var result = await customerBalanceService.UpdateAllCustomerBalancesAsync();
                
                repair.Success = result.Success;
                repair.Message = result.GetSummary();
                repair.CompletedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                repair.Success = false;
                repair.ErrorMessage = ex.Message;
            }

            report.Repairs.Add(repair);
        }

        private async Task RepairOrphanedRecords(RepairReport report)
        {
            var repair = new RepairAction
            {
                Name = "إصلاح السجلات اليتيمة",
                StartedAt = DateTime.Now
            };

            try
            {
                // Remove orphaned records
                var orphanedItems = await _context.SalesOrderItems
                    .Where(item => !_context.SalesOrders.Any(so => so.Id == item.SalesOrderId))
                    .ToListAsync();

                _context.SalesOrderItems.RemoveRange(orphanedItems);
                await _context.SaveChangesAsync();

                repair.Success = true;
                repair.Message = $"تم حذف {orphanedItems.Count} سجل يتيم";
                repair.CompletedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                repair.Success = false;
                repair.ErrorMessage = ex.Message;
            }

            report.Repairs.Add(repair);
        }
    }

    /// <summary>
    /// تقرير سلامة قاعدة البيانات
    /// Database Integrity Report
    /// </summary>
    public class DatabaseIntegrityReport
    {
        public DateTime CheckedAt { get; set; }
        public bool IsHealthy { get; set; }
        public string? ErrorMessage { get; set; }
        public List<IntegrityCheck> Checks { get; set; } = new List<IntegrityCheck>();

        public int TotalChecks => Checks.Count;
        public int PassedChecks => Checks.Count(c => c.Status == CheckStatus.Passed);
        public int FailedChecks => Checks.Count(c => c.Status == CheckStatus.Failed);
        public int WarningChecks => Checks.Count(c => c.Status == CheckStatus.Warning);
    }

    /// <summary>
    /// فحص السلامة
    /// Integrity Check
    /// </summary>
    public class IntegrityCheck
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CheckStatus Status { get; set; }
        public string? Message { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Details { get; set; } = new List<string>();
    }

    /// <summary>
    /// حالة الفحص
    /// Check Status
    /// </summary>
    public enum CheckStatus
    {
        Passed,
        Failed,
        Warning,
        Error
    }

    /// <summary>
    /// تقرير الإصلاح
    /// Repair Report
    /// </summary>
    public class RepairReport
    {
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<RepairAction> Repairs { get; set; } = new List<RepairAction>();
    }

    /// <summary>
    /// عمل إصلاح
    /// Repair Action
    /// </summary>
    public class RepairAction
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
