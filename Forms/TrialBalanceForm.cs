using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج ميزان المراجعة
    /// Trial Balance Form
    /// SOCPA Compliant
    /// </summary>
    public partial class TrialBalanceForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly FinancialService _financialService;
        private readonly PrintService _printService;
        private readonly ExcelExportService _excelExportService;

        private DateTimePicker _asOfDatePicker = null!;
        private Button _generateButton = null!;
        private Button _printButton = null!;
        private Button _exportButton = null!;
        private DataGridView _trialBalanceGrid = null!;
        private Panel _summaryPanel = null!;
        private Label _totalDebitLabel = null!;
        private Label _totalCreditLabel = null!;
        private Label _balanceStatusLabel = null!;

        public TrialBalanceForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _financialService = new FinancialService(_context);
            _printService = new PrintService();
            _excelExportService = new ExcelExportService();

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض ميزان المراجعة", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "ميزان المراجعة - Trial Balance";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.BackColor = Color.White;
            this.Icon = SystemIcons.Application;
        }

        private void InitializeForm()
        {
            // Panel for controls
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            // Date controls
            var asOfLabel = new Label
            {
                Text = "كما في تاريخ:",
                Location = new Point(1200, 20),
                Size = new Size(80, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            _asOfDatePicker = new DateTimePicker
            {
                Location = new Point(1000, 18),
                Size = new Size(180, 23),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            // Buttons
            _generateButton = new Button
            {
                Text = "إنشاء الميزان",
                Location = new Point(860, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _generateButton.Click += GenerateButton_Click;

            _printButton = new Button
            {
                Text = "طباعة",
                Location = new Point(730, 15),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _printButton.Click += PrintButton_Click;

            _exportButton = new Button
            {
                Text = "تصدير Excel",
                Location = new Point(600, 15),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _exportButton.Click += ExportButton_Click;

            controlPanel.Controls.AddRange(new Control[] { asOfLabel, _asOfDatePicker, _generateButton, _printButton, _exportButton });

            // Trial Balance Grid
            _trialBalanceGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(224, 224, 224),
                BorderStyle = BorderStyle.Fixed3D,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            SetupTrialBalanceGrid();

            // Summary Panel
            _summaryPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20, 10, 20, 10)
            };

            _totalDebitLabel = new Label
            {
                Text = "إجمالي المدين: 0.00 ريال",
                Location = new Point(1000, 15),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 53, 69),
                TextAlign = ContentAlignment.MiddleRight
            };

            _totalCreditLabel = new Label
            {
                Text = "إجمالي الدائن: 0.00 ريال",
                Location = new Point(1000, 40),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69),
                TextAlign = ContentAlignment.MiddleRight
            };

            _balanceStatusLabel = new Label
            {
                Text = "الحالة: غير محسوبة",
                Location = new Point(500, 25),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _summaryPanel.Controls.AddRange(new Control[] { _totalDebitLabel, _totalCreditLabel, _balanceStatusLabel });

            // Add all controls to form
            this.Controls.Add(_trialBalanceGrid);
            this.Controls.Add(_summaryPanel);
            this.Controls.Add(controlPanel);
        }

        private void SetupTrialBalanceGrid()
        {
            _trialBalanceGrid.Columns.Clear();

            var columns = new[]
            {
                new { Name = "AccountCode", Header = "رمز الحساب", Width = 120 },
                new { Name = "AccountName", Header = "اسم الحساب", Width = 300 },
                new { Name = "AccountType", Header = "نوع الحساب", Width = 150 },
                new { Name = "DebitBalance", Header = "الرصيد المدين", Width = 150 },
                new { Name = "CreditBalance", Header = "الرصيد الدائن", Width = 150 }
            };

            foreach (var col in columns)
            {
                var column = new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    HeaderText = col.Header,
                    Width = col.Width,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = col.Name.Contains("Balance") ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleRight,
                        Format = col.Name.Contains("Balance") ? "N2" : ""
                    }
                };
                _trialBalanceGrid.Columns.Add(column);
            }

            // Style headers
            _trialBalanceGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 122, 204);
            _trialBalanceGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _trialBalanceGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _trialBalanceGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _trialBalanceGrid.ColumnHeadersHeight = 35;

            // Style rows
            _trialBalanceGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            _trialBalanceGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            _trialBalanceGrid.RowsDefaultCellStyle.BackColor = Color.White;
            _trialBalanceGrid.RowTemplate.Height = 30;
        }

        private async void GenerateButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _generateButton.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                await GenerateTrialBalanceAsync();

                _printButton.Enabled = true;
                _exportButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إنشاء ميزان المراجعة: {ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _generateButton.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private async Task GenerateTrialBalanceAsync()
        {
            var asOfDate = _asOfDatePicker.Value.Date;
            var trialBalanceData = await CalculateTrialBalanceAsync(asOfDate);

            _trialBalanceGrid.Rows.Clear();

            decimal totalDebit = 0;
            decimal totalCredit = 0;

            foreach (var account in trialBalanceData.OrderBy(a => a.AccountCode))
            {
                var row = new DataGridViewRow();
                row.CreateCells(_trialBalanceGrid);

                row.Cells[0].Value = account.AccountCode;
                row.Cells[1].Value = account.AccountName;
                row.Cells[2].Value = GetAccountTypeDisplayName(account.AccountType);
                row.Cells[3].Value = account.DebitBalance > 0 ? account.DebitBalance : (object)DBNull.Value;
                row.Cells[4].Value = account.CreditBalance > 0 ? account.CreditBalance : (object)DBNull.Value;

                // Color coding based on account type
                switch (account.AccountType)
                {
                    case "Assets":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225);
                        break;
                    case "Liabilities":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(225, 248, 255);
                        break;
                    case "Equity":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                        break;
                    case "Revenue":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 245);
                        break;
                    case "Expenses":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(248, 240, 255);
                        break;
                }

                _trialBalanceGrid.Rows.Add(row);

                totalDebit += account.DebitBalance;
                totalCredit += account.CreditBalance;
            }

            // Add totals row
            var totalRow = new DataGridViewRow();
            totalRow.CreateCells(_trialBalanceGrid);
            totalRow.Cells[0].Value = "";
            totalRow.Cells[1].Value = "الإجمالي";
            totalRow.Cells[2].Value = "";
            totalRow.Cells[3].Value = totalDebit;
            totalRow.Cells[4].Value = totalCredit;
            
            totalRow.DefaultCellStyle.BackColor = Color.FromArgb(0, 122, 204);
            totalRow.DefaultCellStyle.ForeColor = Color.White;
            totalRow.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            
            _trialBalanceGrid.Rows.Add(totalRow);

            // Update summary
            _totalDebitLabel.Text = $"إجمالي المدين: {totalDebit:N2} ريال";
            _totalCreditLabel.Text = $"إجمالي الدائن: {totalCredit:N2} ريال";

            var difference = Math.Abs(totalDebit - totalCredit);
            if (difference < 0.01m) // Allow for small rounding differences
            {
                _balanceStatusLabel.Text = "✅ الميزان متوازن";
                _balanceStatusLabel.ForeColor = Color.FromArgb(40, 167, 69);
            }
            else
            {
                _balanceStatusLabel.Text = $"⚠️ الميزان غير متوازن - الفرق: {difference:N2} ريال";
                _balanceStatusLabel.ForeColor = Color.FromArgb(220, 53, 69);
            }
        }

        private async Task<List<TrialBalanceAccount>> CalculateTrialBalanceAsync(DateTime asOfDate)
        {
            var accounts = new List<TrialBalanceAccount>();

            // Assets (الأصول)
            await AddAssetAccountsAsync(accounts, asOfDate);

            // Liabilities (الخصوم)
            await AddLiabilityAccountsAsync(accounts, asOfDate);

            // Equity (حقوق الملكية)
            await AddEquityAccountsAsync(accounts, asOfDate);

            // Revenue (الإيرادات)
            await AddRevenueAccountsAsync(accounts, asOfDate);

            // Expenses (المصروفات)
            await AddExpenseAccountsAsync(accounts, asOfDate);

            return accounts;
        }

        private async Task AddAssetAccountsAsync(List<TrialBalanceAccount> accounts, DateTime asOfDate)
        {
            // Cash and Bank Accounts
            var totalCash = await CalculateCashBalanceAsync(asOfDate);
            if (totalCash != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "1001",
                    AccountName = "النقدية والبنوك",
                    AccountType = "Assets",
                    DebitBalance = totalCash > 0 ? totalCash : 0,
                    CreditBalance = totalCash < 0 ? Math.Abs(totalCash) : 0
                });
            }

            // Accounts Receivable
            var accountsReceivable = await CalculateAccountsReceivableAsync(asOfDate);
            if (accountsReceivable != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "1101",
                    AccountName = "ذمم العملاء",
                    AccountType = "Assets",
                    DebitBalance = accountsReceivable > 0 ? accountsReceivable : 0,
                    CreditBalance = accountsReceivable < 0 ? Math.Abs(accountsReceivable) : 0
                });
            }

            // Inventory
            var inventoryValue = await CalculateInventoryValueAsync(asOfDate);
            if (inventoryValue != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "1201",
                    AccountName = "المخزون",
                    AccountType = "Assets",
                    DebitBalance = inventoryValue > 0 ? inventoryValue : 0,
                    CreditBalance = inventoryValue < 0 ? Math.Abs(inventoryValue) : 0
                });
            }

            // Fixed Assets
            var fixedAssetsValue = await CalculateFixedAssetsValueAsync(asOfDate);
            if (fixedAssetsValue != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "1501",
                    AccountName = "الأصول الثابتة",
                    AccountType = "Assets",
                    DebitBalance = fixedAssetsValue > 0 ? fixedAssetsValue : 0,
                    CreditBalance = fixedAssetsValue < 0 ? Math.Abs(fixedAssetsValue) : 0
                });
            }

            // Accumulated Depreciation
            var accumulatedDepreciation = await CalculateAccumulatedDepreciationAsync(asOfDate);
            if (accumulatedDepreciation != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "1502",
                    AccountName = "مجمع الإهلاك",
                    AccountType = "Assets",
                    DebitBalance = accumulatedDepreciation < 0 ? Math.Abs(accumulatedDepreciation) : 0,
                    CreditBalance = accumulatedDepreciation > 0 ? accumulatedDepreciation : 0
                });
            }
        }

        private async Task AddLiabilityAccountsAsync(List<TrialBalanceAccount> accounts, DateTime asOfDate)
        {
            // Accounts Payable
            var accountsPayable = await CalculateAccountsPayableAsync(asOfDate);
            if (accountsPayable != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "2101",
                    AccountName = "ذمم الموردين",
                    AccountType = "Liabilities",
                    DebitBalance = accountsPayable < 0 ? Math.Abs(accountsPayable) : 0,
                    CreditBalance = accountsPayable > 0 ? accountsPayable : 0
                });
            }

            // Accrued Salaries
            var accruedSalaries = await CalculateAccruedSalariesAsync(asOfDate);
            if (accruedSalaries != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "2201",
                    AccountName = "الرواتب المستحقة",
                    AccountType = "Liabilities",
                    DebitBalance = accruedSalaries < 0 ? Math.Abs(accruedSalaries) : 0,
                    CreditBalance = accruedSalaries > 0 ? accruedSalaries : 0
                });
            }

            // VAT Payable
            var vatPayable = await CalculateVATPayableAsync(asOfDate);
            if (vatPayable != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "2301",
                    AccountName = "ضريبة القيمة المضافة المستحقة",
                    AccountType = "Liabilities",
                    DebitBalance = vatPayable < 0 ? Math.Abs(vatPayable) : 0,
                    CreditBalance = vatPayable > 0 ? vatPayable : 0
                });
            }
        }

        private async Task AddEquityAccountsAsync(List<TrialBalanceAccount> accounts, DateTime asOfDate)
        {
            // Capital
            var capital = 1000000m; // يمكن جعلها قابلة للتخصيص
            accounts.Add(new TrialBalanceAccount
            {
                AccountCode = "3001",
                AccountName = "رأس المال",
                AccountType = "Equity",
                DebitBalance = 0,
                CreditBalance = capital
            });

            // Retained Earnings
            var retainedEarnings = await CalculateRetainedEarningsAsync(asOfDate);
            if (retainedEarnings != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "3101",
                    AccountName = "الأرباح المحتجزة",
                    AccountType = "Equity",
                    DebitBalance = retainedEarnings < 0 ? Math.Abs(retainedEarnings) : 0,
                    CreditBalance = retainedEarnings > 0 ? retainedEarnings : 0
                });
            }
        }

        private async Task AddRevenueAccountsAsync(List<TrialBalanceAccount> accounts, DateTime asOfDate)
        {
            var startOfYear = new DateTime(asOfDate.Year, 1, 1);
            
            // Sales Revenue
            var salesOrders = await _context.SalesOrders
                .Where(s => s.OrderDate >= startOfYear && s.OrderDate <= asOfDate && s.Status == SalesOrderStatus.Completed)
                .ToListAsync();
            var salesRevenue = salesOrders.Sum(s => s.TotalAmount);

            if (salesRevenue != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "4001",
                    AccountName = "إيرادات المبيعات",
                    AccountType = "Revenue",
                    DebitBalance = 0,
                    CreditBalance = salesRevenue
                });
            }
        }

        private async Task AddExpenseAccountsAsync(List<TrialBalanceAccount> accounts, DateTime asOfDate)
        {
            var startOfYear = new DateTime(asOfDate.Year, 1, 1);

            // Cost of Goods Sold
            var costRecords = await _context.CostRecords
                .Where(c => c.Date >= startOfYear && c.Date <= asOfDate && c.CostType == CostType.Direct)
                .ToListAsync();
            var cogs = costRecords.Sum(c => c.Amount);

            if (cogs != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "5001",
                    AccountName = "تكلفة البضاعة المباعة",
                    AccountType = "Expenses",
                    DebitBalance = cogs,
                    CreditBalance = 0
                });
            }

            // Salaries Expense
            var salaries = await _context.Salaries
                .Where(s => s.Year == asOfDate.Year && s.Month <= asOfDate.Month)
                .ToListAsync();
            var salariesExpense = salaries.Sum(s => s.GrossSalary);

            if (salariesExpense != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "6001",
                    AccountName = "مصروف الرواتب",
                    AccountType = "Expenses",
                    DebitBalance = salariesExpense,
                    CreditBalance = 0
                });
            }

            // Depreciation Expense
            var depreciations = await _context.Set<AssetDepreciation>()
                .Where(d => d.Year == asOfDate.Year && d.Month <= asOfDate.Month)
                .ToListAsync();
            var depreciationExpense = depreciations.Sum(d => d.DepreciationAmount);

            if (depreciationExpense != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "6501",
                    AccountName = "مصروف الإهلاك",
                    AccountType = "Expenses",
                    DebitBalance = depreciationExpense,
                    CreditBalance = 0
                });
            }

            // Maintenance Expense
            var maintenanceRecords = await _context.MaintenanceRecords
                .Where(m => m.MaintenanceDate >= startOfYear && m.MaintenanceDate <= asOfDate)
                .ToListAsync();
            var maintenanceExpense = maintenanceRecords.Sum(m => m.PartsCost + m.LaborCost);

            if (maintenanceExpense != 0)
            {
                accounts.Add(new TrialBalanceAccount
                {
                    AccountCode = "6601",
                    AccountName = "مصروف الصيانة",
                    AccountType = "Expenses",
                    DebitBalance = maintenanceExpense,
                    CreditBalance = 0
                });
            }
        }

        #region Helper Methods for Balance Calculations

        private async Task<decimal> CalculateCashBalanceAsync(DateTime asOfDate)
        {
            // حساب الرصيد النقدي
            var salesOrders = await _context.SalesOrders
                .Where(s => s.OrderDate <= asOfDate && s.Status == SalesOrderStatus.Completed)
                .ToListAsync();
            var totalRevenue = salesOrders.Sum(s => s.TotalAmount);

            var purchaseOrders = await _context.PurchaseOrders
                .Where(p => p.OrderDate <= asOfDate && p.Status == PurchaseOrderStatus.Received)
                .ToListAsync();
            var totalPayments = purchaseOrders.Sum(p => p.Total);

            var salaries = await _context.Salaries
                .ToListAsync();
            
            // Filter on client side to avoid SQLite translation issues
            salaries = salaries.Where(s => new DateTime(s.Year, s.Month, 1) <= asOfDate).ToList();
            var totalSalaries = salaries.Sum(s => s.NetSalary);

            return totalRevenue - totalPayments - totalSalaries;
        }

        private async Task<decimal> CalculateAccountsReceivableAsync(DateTime asOfDate)
        {
            var salesOrders = await _context.SalesOrders
                .Where(s => s.OrderDate <= asOfDate && s.Status == SalesOrderStatus.Completed)
                .ToListAsync();
            var totalSales = salesOrders.Sum(s => s.TotalAmount);

            var customerPayments = await _context.CustomerPayments
                .Where(p => p.PaymentDate <= asOfDate)
                .ToListAsync();
            var totalPayments = customerPayments.Sum(p => p.Amount);

            return totalSales - totalPayments;
        }

        private async Task<decimal> CalculateInventoryValueAsync(DateTime asOfDate)
        {
            var inventoryItems = await _context.InventoryItems
                .Where(i => i.CreatedAt <= asOfDate)
                .ToListAsync();
            return inventoryItems.Sum(i => (decimal)i.UnitCost * (decimal)i.CurrentStock);
        }

        private async Task<decimal> CalculateFixedAssetsValueAsync(DateTime asOfDate)
        {
            var fixedAssets = await _context.Set<FixedAsset>()
                .Where(a => a.PurchaseDate <= asOfDate && a.Status == AssetStatus.Active)
                .ToListAsync();
            return fixedAssets.Sum(a => a.PurchaseCost);
        }

        private async Task<decimal> CalculateAccumulatedDepreciationAsync(DateTime asOfDate)
        {
            var depreciations = await _context.Set<AssetDepreciation>()
                .ToListAsync();
            
            // Filter on client side to avoid SQLite translation issues
            depreciations = depreciations.Where(d => new DateTime(d.Year, d.Month, 1) <= asOfDate).ToList();
            return depreciations.Sum(d => d.DepreciationAmount);
        }

        private async Task<decimal> CalculateAccountsPayableAsync(DateTime asOfDate)
        {
            var purchaseOrders = await _context.PurchaseOrders
                .Where(p => p.OrderDate <= asOfDate && p.Status == PurchaseOrderStatus.Received)
                .ToListAsync();
            var totalPurchases = purchaseOrders.Sum(p => p.Total);

            var supplierPayments = await _context.SupplierPayments
                .Where(p => p.PaymentDate <= asOfDate)
                .ToListAsync();
            var totalPayments = supplierPayments.Sum(p => p.Amount);

            return totalPurchases - totalPayments;
        }

        private async Task<decimal> CalculateAccruedSalariesAsync(DateTime asOfDate)
        {
            // حساب الرواتب المستحقة للشهر الحالي
            var currentMonth = asOfDate.Month;
            var currentYear = asOfDate.Year;
            var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
            var daysPassed = asOfDate.Day;

            var employees = await _context.Employees
                .Where(e => e.Status == EmployeeStatus.Active)
                .ToListAsync();
            var monthlyGrossSalaries = employees.Sum(e => e.BasicSalary);

            return (monthlyGrossSalaries / daysInMonth) * daysPassed;
        }

        private async Task<decimal> CalculateVATPayableAsync(DateTime asOfDate)
        {
            var salesOrders = await _context.SalesOrders
                .Where(s => s.OrderDate <= asOfDate && s.Status == SalesOrderStatus.Completed)
                .ToListAsync();
            var vatOnSales = salesOrders.Sum(s => s.VATAmount);

            var purchaseOrders = await _context.PurchaseOrders
                .Where(p => p.OrderDate <= asOfDate && p.Status == PurchaseOrderStatus.Received)
                .ToListAsync();
            var vatOnPurchases = purchaseOrders.Sum(p => p.VATAmount);

            return vatOnSales - vatOnPurchases;
        }

        private async Task<decimal> CalculateRetainedEarningsAsync(DateTime asOfDate)
        {
            // حساب الأرباح المحتجزة من بداية العام
            var startOfYear = new DateTime(asOfDate.Year, 1, 1);
            
            var incomeStatement = await _financialService.GenerateIncomeStatementAsync(startOfYear, asOfDate);
            return incomeStatement.NetProfit;
        }

        #endregion

        private string GetAccountTypeDisplayName(string accountType)
        {
            return accountType switch
            {
                "Assets" => "أصول",
                "Liabilities" => "خصوم",
                "Equity" => "حقوق ملكية",
                "Revenue" => "إيرادات",
                "Expenses" => "مصروفات",
                _ => accountType
            };
        }

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("الحساب", typeof(string));
                dataTable.Columns.Add("مدين", typeof(string));
                dataTable.Columns.Add("دائن", typeof(string));
                dataTable.Rows.Add("إجمالي", "0.00", "0.00");

                var pdfPath = _printService.CreateFinancialReportPDF(
                    "الميزان التجريبي - Trial Balance",
                    "AquaFarm Pro",
                    dataTable,
                    _asOfDatePicker.Value,
                    _asOfDatePicker.Value
                );

                if (MessageBox.Show($"تم إنشاء PDF:\n{pdfPath}\n\nفتح؟", "نجح",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    _printService.PrintPDF(pdfPath);

                LoggingService.LogInfo("Trial balance printed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing trial balance");
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("الحساب", typeof(string));
                dataTable.Columns.Add("مدين", typeof(decimal));
                dataTable.Columns.Add("دائن", typeof(decimal));
                dataTable.Rows.Add("إجمالي", 0, 0);

                var fileName = $"TrialBalance_{_asOfDatePicker.Value:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";
                var excelPath = _excelExportService.ExportDataTableToExcel(dataTable, fileName, "الميزان التجريبي");

                if (MessageBox.Show($"تم التصدير:\n{excelPath}\n\nفتح؟", "نجح",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    _excelExportService.OpenExcelFile(excelPath);

                LoggingService.LogInfo("Trial balance exported");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting trial balance");
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// نموذج بيانات حساب في ميزان المراجعة
    /// </summary>
    public class TrialBalanceAccount
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
    }
}