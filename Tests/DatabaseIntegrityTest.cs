using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests
{
    /// <summary>
    /// اختبار سلامة قاعدة البيانات
    /// Database Integrity Test
    /// </summary>
    public class DatabaseIntegrityTest
    {
        private FishFarmContext _context;
        private DatabaseIntegrityService _integrityService;
        private CustomerBalanceService _balanceService;
        private TaxInvoiceService _taxInvoiceService;

        public DatabaseIntegrityTest()
        {
            var options = new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite("Data Source=test_fishfarm.db")
                .Options;

            _context = new FishFarmContext(options);
            _integrityService = new DatabaseIntegrityService(_context);
            _balanceService = new CustomerBalanceService(_context);
            _taxInvoiceService = new TaxInvoiceService(_context);
        }

        /// <summary>
        /// اختبار إنشاء عميل جديد
        /// Test creating new customer
        /// </summary>
        public async Task<bool> TestCreateCustomer()
        {
            try
            {
                var customer = new Customer
                {
                    Name = "عميل اختبار",
                    Email = "test@example.com",
                    Phone = "0501234567",
                    Address = "الرياض، المملكة العربية السعودية",
                    Status = CustomerStatus.Active,
                    Type = CustomerType.Individual,
                    CurrentBalance = 0,
                    CreditLimit = 10000,
                    PaymentTermDays = 30,
                    CreatedAt = DateTime.Now
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return customer.Id > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// اختبار إنشاء أمر مبيعات
        /// Test creating sales order
        /// </summary>
        public async Task<bool> TestCreateSalesOrder(int customerId)
        {
            try
            {
                var salesOrder = new SalesOrder
                {
                    OrderNumber = "SO-TEST-001",
                    OrderDate = DateTime.Now,
                    CustomerId = customerId,
                    Status = SalesOrderStatus.Pending,
                    SubTotal = 1000,
                    DiscountAmount = 0,
                    TaxAmount = 150,
                    VATAmount = 150,
                    GrandTotal = 1150,
                    TotalAmount = 1150,
                    PaidAmount = 0,
                    RemainingAmount = 1150,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "Test User"
                };

                _context.SalesOrders.Add(salesOrder);
                await _context.SaveChangesAsync();

                // Test customer balance update
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    customer.CurrentBalance += salesOrder.TotalAmount;
                    await _context.SaveChangesAsync();
                }

                return salesOrder.Id > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// اختبار إنشاء فاتورة ضريبية
        /// Test creating tax invoice
        /// </summary>
        public async Task<bool> TestCreateTaxInvoice(int salesOrderId)
        {
            try
            {
                var taxInvoice = await _taxInvoiceService.CreateTaxInvoiceFromSalesOrderAsync(salesOrderId);
                return taxInvoice != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// اختبار حساب رصيد العميل
        /// Test customer balance calculation
        /// </summary>
        public async Task<bool> TestCustomerBalanceCalculation(int customerId)
        {
            try
            {
                var balanceInfo = await _balanceService.CalculateCustomerBalanceAsync(customerId);
                return balanceInfo.IsValid;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// اختبار سلامة قاعدة البيانات
        /// Test database integrity
        /// </summary>
        public async Task<bool> TestDatabaseIntegrity()
        {
            try
            {
                var report = await _integrityService.CheckDatabaseIntegrityAsync();
                return report.IsHealthy;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// تشغيل جميع الاختبارات
        /// Run all tests
        /// </summary>
        public async Task<TestResults> RunAllTests()
        {
            var results = new TestResults
            {
                TestedAt = DateTime.Now,
                Tests = new List<TestResult>()
            };

            // Test 1: Create Customer
            var customerTest = new TestResult
            {
                Name = "إنشاء عميل جديد",
                Description = "اختبار إنشاء عميل جديد في قاعدة البيانات"
            };
            customerTest.Passed = await TestCreateCustomer();
            results.Tests.Add(customerTest);

            if (customerTest.Passed)
            {
                // Get the created customer ID
                var customer = await _context.Customers.OrderByDescending(c => c.Id).FirstOrDefaultAsync();
                if (customer != null)
                {
                    // Test 2: Create Sales Order
                    var salesOrderTest = new TestResult
                    {
                        Name = "إنشاء أمر مبيعات",
                        Description = "اختبار إنشاء أمر مبيعات وربطه بالعميل"
                    };
                    salesOrderTest.Passed = await TestCreateSalesOrder(customer.Id);
                    results.Tests.Add(salesOrderTest);

                    if (salesOrderTest.Passed)
                    {
                        // Get the created sales order ID
                        var salesOrder = await _context.SalesOrders.OrderByDescending(so => so.Id).FirstOrDefaultAsync();
                        if (salesOrder != null)
                        {
                            // Test 3: Create Tax Invoice
                            var taxInvoiceTest = new TestResult
                            {
                                Name = "إنشاء فاتورة ضريبية",
                                Description = "اختبار إنشاء فاتورة ضريبية تلقائياً من أمر المبيعات"
                            };
                            taxInvoiceTest.Passed = await TestCreateTaxInvoice(salesOrder.Id);
                            results.Tests.Add(taxInvoiceTest);
                        }
                    }

                    // Test 4: Customer Balance Calculation
                    var balanceTest = new TestResult
                    {
                        Name = "حساب رصيد العميل",
                        Description = "اختبار حساب رصيد العميل من المعاملات"
                    };
                    balanceTest.Passed = await TestCustomerBalanceCalculation(customer.Id);
                    results.Tests.Add(balanceTest);
                }
            }

            // Test 5: Database Integrity
            var integrityTest = new TestResult
            {
                Name = "سلامة قاعدة البيانات",
                Description = "اختبار سلامة قاعدة البيانات والعلاقات"
            };
            integrityTest.Passed = await TestDatabaseIntegrity();
            results.Tests.Add(integrityTest);

            results.TotalTests = results.Tests.Count;
            results.PassedTests = results.Tests.Count(t => t.Passed);
            results.FailedTests = results.Tests.Count(t => !t.Passed);
            results.Success = results.FailedTests == 0;

            return results;
        }

        /// <summary>
        /// تنظيف بيانات الاختبار
        /// Clean up test data
        /// </summary>
        public async Task CleanupTestData()
        {
            try
            {
                // Remove test tax invoices
                var testTaxInvoices = await _context.TaxInvoices
                    .Where(ti => ti.InvoiceNumber.StartsWith("TI-TEST"))
                    .ToListAsync();
                _context.TaxInvoices.RemoveRange(testTaxInvoices);

                // Remove test sales orders
                var testSalesOrders = await _context.SalesOrders
                    .Where(so => so.OrderNumber.StartsWith("SO-TEST"))
                    .ToListAsync();
                _context.SalesOrders.RemoveRange(testSalesOrders);

                // Remove test customers
                var testCustomers = await _context.Customers
                    .Where(c => c.Name.Contains("اختبار"))
                    .ToListAsync();
                _context.Customers.RemoveRange(testCustomers);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    /// <summary>
    /// اختبارات دخان قابلة للاكتشاف بواسطة dotnet test.
    /// </summary>
    public sealed class DatabaseSmokeTests
    {
        [Fact]
        public async Task CanCreateSchemaAndPersistCustomer()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"aquafarm-test-{Guid.NewGuid():N}.db");

            try
            {
                var options = new DbContextOptionsBuilder<FishFarmContext>()
                    .UseSqlite($"Data Source={databasePath};Pooling=False")
                    .Options;

                await using var context = new FishFarmContext(options);
                await context.Database.EnsureCreatedAsync();

                var customer = new Customer
                {
                    Name = "Test Customer",
                    Type = CustomerType.Individual,
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                context.Customers.Add(customer);
                await context.SaveChangesAsync();

                Assert.True(customer.Id > 0);
                Assert.Equal(1, await context.Customers.CountAsync());
            }
            finally
            {
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }
    }

    /// <summary>
    /// نتائج الاختبار
    /// Test Results
    /// </summary>
    public class TestResults
    {
        public DateTime TestedAt { get; set; }
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public List<TestResult> Tests { get; set; } = new List<TestResult>();

        public string GetSummary()
        {
            return $"تم تشغيل {TotalTests} اختبار: نجح {PassedTests}، فشل {FailedTests}";
        }
    }

    /// <summary>
    /// نتيجة اختبار واحد
    /// Single Test Result
    /// </summary>
    public class TestResult
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime TestedAt { get; set; } = DateTime.Now;
    }
}
