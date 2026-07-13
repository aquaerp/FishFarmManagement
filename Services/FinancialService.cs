using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة التقارير المالية والحسابات
    /// Financial Reports and Calculations Service
    /// SOCPA Compliant
    /// </summary>
    public class FinancialService
    {
        private readonly FishFarmContext _context;

        public FinancialService(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Income Statement (قائمة الدخل)

        /// <summary>
        /// إنشاء قائمة الدخل للفترة المحددة
        /// </summary>
        public async Task<IncomeStatementData> GenerateIncomeStatementAsync(DateTime startDate, DateTime endDate)
        {
            var data = new IncomeStatementData
            {
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.Now
            };

            // الإيرادات (Revenues)
            data.SalesRevenue = await CalculateSalesRevenueAsync(startDate, endDate);
            data.OtherRevenue = await CalculateOtherRevenueAsync(startDate, endDate);
            data.TotalRevenue = data.SalesRevenue + data.OtherRevenue;

            // تكلفة المبيعات (Cost of Goods Sold)
            data.CostOfGoodsSold = await CalculateCOGSAsync(startDate, endDate);
            data.GrossProfit = data.TotalRevenue - data.CostOfGoodsSold;
            data.GrossProfitMargin = data.TotalRevenue > 0 ? (data.GrossProfit / data.TotalRevenue) * 100 : 0;

            // المصروفات التشغيلية (Operating Expenses)
            data.SalariesExpense = await CalculateSalariesExpenseAsync(startDate, endDate);
            data.DepreciationExpense = await CalculateDepreciationExpenseAsync(startDate, endDate);
            data.UtilitiesExpense = await CalculateUtilitiesExpenseAsync(startDate, endDate);
            data.MaintenanceExpense = await CalculateMaintenanceExpenseAsync(startDate, endDate);
            data.OtherOperatingExpenses = await CalculateOtherExpensesAsync(startDate, endDate);
            data.TotalOperatingExpenses = data.SalariesExpense + data.DepreciationExpense + 
                data.UtilitiesExpense + data.MaintenanceExpense + data.OtherOperatingExpenses;

            // الربح التشغيلي (Operating Profit)
            data.OperatingProfit = data.GrossProfit - data.TotalOperatingExpenses;
            data.OperatingProfitMargin = data.TotalRevenue > 0 ? (data.OperatingProfit / data.TotalRevenue) * 100 : 0;

            // الإيرادات والمصروفات الأخرى (Other Income/Expenses)
            data.InterestIncome = 0; // يمكن إضافتها لاحقاً
            data.InterestExpense = 0;
            data.OtherIncome = 0;
            data.OtherExpenses = 0;

            // صافي الربح قبل الضريبة (Net Profit Before Tax)
            data.NetProfitBeforeTax = data.OperatingProfit + data.InterestIncome + data.OtherIncome - 
                data.InterestExpense - data.OtherExpenses;

            // ضريبة الدخل (Income Tax) - افتراضياً 0 للشركات الصغيرة
            data.IncomeTax = 0;

            // صافي الربح (Net Profit)
            data.NetProfit = data.NetProfitBeforeTax - data.IncomeTax;
            data.NetProfitMargin = data.TotalRevenue > 0 ? (data.NetProfit / data.TotalRevenue) * 100 : 0;

            return data;
        }

        private async Task<decimal> CalculateSalesRevenueAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.SalesOrders
                .Where(s => s.OrderDate >= startDate && s.OrderDate <= endDate && s.Status == SalesOrderStatus.Completed)
                .ToListAsync();
            return orders.Sum(s => s.TotalAmount);
        }

        private async Task<decimal> CalculateOtherRevenueAsync(DateTime startDate, DateTime endDate)
        {
            // يمكن إضافة مصادر إيرادات أخرى
            return 0;
        }

        private async Task<decimal> CalculateCOGSAsync(DateTime startDate, DateTime endDate)
        {
            // تكلفة البضاعة المباعة = تكلفة المخزون المستخدم في الإنتاج + مصاريف إنتاج مباشرة
            var costs = await _context.CostRecords
                .Where(c => c.Date >= startDate && c.Date <= endDate && c.CostType == CostType.Direct)
                .ToListAsync();
            return costs.Sum(c => c.Amount);
        }

        private async Task<decimal> CalculateSalariesExpenseAsync(DateTime startDate, DateTime endDate)
        {
            var startMonth = startDate.Month;
            var startYear = startDate.Year;
            var endMonth = endDate.Month;
            var endYear = endDate.Year;

            var salaries = await _context.Salaries
                .Where(s => (s.Year == startYear && s.Month >= startMonth) || 
                           (s.Year == endYear && s.Month <= endMonth))
                .ToListAsync();
            return salaries.Sum(s => s.NetSalary);
        }

        private async Task<decimal> CalculateDepreciationExpenseAsync(DateTime startDate, DateTime endDate)
        {
            var startMonth = startDate.Month;
            var startYear = startDate.Year;
            var endMonth = endDate.Month;
            var endYear = endDate.Year;

            var depreciations = await _context.Set<AssetDepreciation>()
                .Where(d => (d.Year == startYear && d.Month >= startMonth) || 
                           (d.Year == endYear && d.Month <= endMonth))
                .ToListAsync();
            return depreciations.Sum(d => d.DepreciationAmount);
        }

        private async Task<decimal> CalculateUtilitiesExpenseAsync(DateTime startDate, DateTime endDate)
        {
            var costs = await _context.CostRecords
                .Where(c => c.Date >= startDate && c.Date <= endDate && c.CostType == CostType.Indirect)
                .ToListAsync();
            return costs.Sum(c => c.Amount);
        }

        private async Task<decimal> CalculateMaintenanceExpenseAsync(DateTime startDate, DateTime endDate)
        {
            var records = await _context.MaintenanceRecords
                .Where(m => m.MaintenanceDate >= startDate && m.MaintenanceDate <= endDate)
                .ToListAsync();
            return records.Sum(m => m.PartsCost + m.LaborCost);
        }

        private async Task<decimal> CalculateOtherExpensesAsync(DateTime startDate, DateTime endDate)
        {
            var costs = await _context.CostRecords
                .Where(c => c.Date >= startDate && c.Date <= endDate && c.CostType == CostType.Fixed)
                .ToListAsync();
            return costs.Sum(c => c.Amount);
        }

        #endregion

        #region Balance Sheet (الميزانية العمومية)

        /// <summary>
        /// إنشاء الميزانية العمومية في تاريخ محدد
        /// </summary>
        public async Task<BalanceSheetData> GenerateBalanceSheetAsync(DateTime asOfDate)
        {
            var data = new BalanceSheetData
            {
                AsOfDate = asOfDate,
                GeneratedDate = DateTime.Now
            };

            // الأصول المتداولة (Current Assets)
            data.Cash = 100000; // يمكن ربطها بنظام الخزينة
            data.AccountsReceivable = await CalculateAccountsReceivableAsync(asOfDate);
            data.Inventory = await CalculateInventoryValueAsync(asOfDate);
            data.OtherCurrentAssets = 0;
            data.TotalCurrentAssets = data.Cash + data.AccountsReceivable + data.Inventory + data.OtherCurrentAssets;

            // الأصول الثابتة (Fixed Assets)
            data.FixedAssetsGross = await CalculateFixedAssetsGrossAsync(asOfDate);
            data.AccumulatedDepreciation = await CalculateAccumulatedDepreciationAsync(asOfDate);
            data.FixedAssetsNet = data.FixedAssetsGross - data.AccumulatedDepreciation;

            // إجمالي الأصول (Total Assets)
            data.TotalAssets = data.TotalCurrentAssets + data.FixedAssetsNet;

            // الخصوم المتداولة (Current Liabilities)
            data.AccountsPayable = await CalculateAccountsPayableAsync(asOfDate);
            data.SalariesPayable = await CalculateSalariesPayableAsync(asOfDate);
            data.TaxesPayable = 0;
            data.OtherCurrentLiabilities = 0;
            data.TotalCurrentLiabilities = data.AccountsPayable + data.SalariesPayable + 
                data.TaxesPayable + data.OtherCurrentLiabilities;

            // الخصوم طويلة الأجل (Long-term Liabilities)
            data.LongTermLoans = 0;
            data.OtherLongTermLiabilities = 0;
            data.TotalLongTermLiabilities = data.LongTermLoans + data.OtherLongTermLiabilities;

            // إجمالي الخصوم (Total Liabilities)
            data.TotalLiabilities = data.TotalCurrentLiabilities + data.TotalLongTermLiabilities;

            // حقوق الملكية (Equity)
            data.Capital = 500000; // رأس المال المدفوع
            data.RetainedEarnings = await CalculateRetainedEarningsAsync(asOfDate);
            data.CurrentYearProfit = await CalculateCurrentYearProfitAsync(asOfDate);
            data.TotalEquity = data.Capital + data.RetainedEarnings + data.CurrentYearProfit;

            // إجمالي الخصوم وحقوق الملكية
            data.TotalLiabilitiesAndEquity = data.TotalLiabilities + data.TotalEquity;

            // التحقق من المعادلة المحاسبية
            data.IsBalanced = Math.Abs(data.TotalAssets - data.TotalLiabilitiesAndEquity) < 0.01m;

            return data;
        }

        private async Task<decimal> CalculateAccountsReceivableAsync(DateTime asOfDate)
        {
            var orders = await _context.SalesOrders
                .Where(s => s.OrderDate <= asOfDate && s.RemainingAmount > 0)
                .ToListAsync();
            return orders.Sum(s => s.RemainingAmount);
        }

        private async Task<decimal> CalculateInventoryValueAsync(DateTime asOfDate)
        {
            var items = await _context.InventoryItems
                .Where(i => i.IsActive)
                .ToListAsync();
            return items.Sum(i => (decimal)i.CurrentStock * (decimal)i.UnitCost);
        }

        private async Task<decimal> CalculateFixedAssetsGrossAsync(DateTime asOfDate)
        {
            var assets = await _context.Set<FixedAsset>()
                .Where(a => a.PurchaseDate <= asOfDate && a.Status != AssetStatus.Sold && a.Status != AssetStatus.Disposed)
                .ToListAsync();
            return assets.Sum(a => a.PurchaseCost);
        }

        private async Task<decimal> CalculateAccumulatedDepreciationAsync(DateTime asOfDate)
        {
            var year = asOfDate.Year;
            var month = asOfDate.Month;

            var depreciations = await _context.Set<AssetDepreciation>()
                .Where(d => d.Year < year || (d.Year == year && d.Month <= month))
                .ToListAsync();
            return depreciations.Sum(d => d.DepreciationAmount);
        }

        private async Task<decimal> CalculateAccountsPayableAsync(DateTime asOfDate)
        {
            var orders = await _context.PurchaseOrders
                .Where(p => p.OrderDate <= asOfDate && p.Status != PurchaseOrderStatus.Closed)
                .ToListAsync();
            return orders.Sum(p => p.Total);
        }

        private async Task<decimal> CalculateSalariesPayableAsync(DateTime asOfDate)
        {
            var month = asOfDate.Month;
            var year = asOfDate.Year;

            var salaries = await _context.Salaries
                .Where(s => s.Year == year && s.Month == month && s.PaidDate == null)
                .ToListAsync();
            return salaries.Sum(s => s.NetSalary);
        }

        private async Task<decimal> CalculateRetainedEarningsAsync(DateTime asOfDate)
        {
            // الأرباح المحتجزة من السنوات السابقة
            var previousYears = asOfDate.Year - 1;
            var startDate = new DateTime(2020, 1, 1);
            var endDate = new DateTime(previousYears, 12, 31);

            if (endDate < startDate)
                return 0;

            var incomeStatement = await GenerateIncomeStatementAsync(startDate, endDate);
            return incomeStatement.NetProfit;
        }

        private async Task<decimal> CalculateCurrentYearProfitAsync(DateTime asOfDate)
        {
            var startDate = new DateTime(asOfDate.Year, 1, 1);
            var incomeStatement = await GenerateIncomeStatementAsync(startDate, asOfDate);
            return incomeStatement.NetProfit;
        }

        #endregion

        #region Cash Flow (التدفقات النقدية)

        /// <summary>
        /// إنشاء قائمة التدفقات النقدية
        /// </summary>
        public async Task<CashFlowData> GenerateCashFlowStatementAsync(DateTime startDate, DateTime endDate)
        {
            var data = new CashFlowData
            {
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.Now
            };

            // الأنشطة التشغيلية (Operating Activities)
            var incomeStatement = await GenerateIncomeStatementAsync(startDate, endDate);
            data.NetProfit = incomeStatement.NetProfit;
            data.DepreciationAddBack = incomeStatement.DepreciationExpense;
            data.ChangeInReceivables = -await CalculateChangeInReceivablesAsync(startDate, endDate);
            data.ChangeInInventory = -await CalculateChangeInInventoryAsync(startDate, endDate);
            data.ChangeInPayables = await CalculateChangeInPayablesAsync(startDate, endDate);
            data.NetCashFromOperating = data.NetProfit + data.DepreciationAddBack + 
                data.ChangeInReceivables + data.ChangeInInventory + data.ChangeInPayables;

            // الأنشطة الاستثمارية (Investing Activities)
            data.PurchaseOfFixedAssets = -await CalculateFixedAssetPurchasesAsync(startDate, endDate);
            data.SaleOfFixedAssets = await CalculateFixedAssetSalesAsync(startDate, endDate);
            data.NetCashFromInvesting = data.PurchaseOfFixedAssets + data.SaleOfFixedAssets;

            // الأنشطة التمويلية (Financing Activities)
            data.CapitalContributions = 0;
            data.LoanProceeds = 0;
            data.LoanRepayments = 0;
            data.Dividends = 0;
            data.NetCashFromFinancing = data.CapitalContributions + data.LoanProceeds - data.LoanRepayments - data.Dividends;

            // صافي التغير في النقدية
            data.NetCashChange = data.NetCashFromOperating + data.NetCashFromInvesting + data.NetCashFromFinancing;
            data.BeginningCash = 100000; // يمكن حسابها من الميزانية السابقة
            data.EndingCash = data.BeginningCash + data.NetCashChange;

            return data;
        }

        private async Task<decimal> CalculateChangeInReceivablesAsync(DateTime startDate, DateTime endDate)
        {
            var beginningAR = await CalculateAccountsReceivableAsync(startDate.AddDays(-1));
            var endingAR = await CalculateAccountsReceivableAsync(endDate);
            return endingAR - beginningAR;
        }

        private async Task<decimal> CalculateChangeInInventoryAsync(DateTime startDate, DateTime endDate)
        {
            var beginningInv = await CalculateInventoryValueAsync(startDate.AddDays(-1));
            var endingInv = await CalculateInventoryValueAsync(endDate);
            return endingInv - beginningInv;
        }

        private async Task<decimal> CalculateChangeInPayablesAsync(DateTime startDate, DateTime endDate)
        {
            var beginningAP = await CalculateAccountsPayableAsync(startDate.AddDays(-1));
            var endingAP = await CalculateAccountsPayableAsync(endDate);
            return endingAP - beginningAP;
        }

        private async Task<decimal> CalculateFixedAssetPurchasesAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Set<FixedAsset>()
                .Where(a => a.PurchaseDate >= startDate && a.PurchaseDate <= endDate)
                .SumAsync(a => a.PurchaseCost);
        }

        private async Task<decimal> CalculateFixedAssetSalesAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Set<FixedAsset>()
                .Where(a => a.DisposalDate >= startDate && a.DisposalDate <= endDate && a.DisposalValue.HasValue)
                .SumAsync(a => a.DisposalValue ?? 0);
        }

        #endregion

        #region Financial Ratios (النسب المالية)

        /// <summary>
        /// حساب النسب المالية
        /// </summary>
        public async Task<FinancialRatios> CalculateFinancialRatiosAsync(DateTime asOfDate)
        {
            var balanceSheet = await GenerateBalanceSheetAsync(asOfDate);
            var startOfYear = new DateTime(asOfDate.Year, 1, 1);
            var incomeStatement = await GenerateIncomeStatementAsync(startOfYear, asOfDate);

            var ratios = new FinancialRatios
            {
                // نسب الربحية (Profitability Ratios)
                GrossProfitMargin = incomeStatement.GrossProfitMargin,
                OperatingProfitMargin = incomeStatement.OperatingProfitMargin,
                NetProfitMargin = incomeStatement.NetProfitMargin,
                ROA = balanceSheet.TotalAssets > 0 ? (incomeStatement.NetProfit / balanceSheet.TotalAssets) * 100 : 0,
                ROE = balanceSheet.TotalEquity > 0 ? (incomeStatement.NetProfit / balanceSheet.TotalEquity) * 100 : 0,

                // نسب السيولة (Liquidity Ratios)
                CurrentRatio = balanceSheet.TotalCurrentLiabilities > 0 ? balanceSheet.TotalCurrentAssets / balanceSheet.TotalCurrentLiabilities : 0,
                QuickRatio = balanceSheet.TotalCurrentLiabilities > 0 ? (balanceSheet.TotalCurrentAssets - balanceSheet.Inventory) / balanceSheet.TotalCurrentLiabilities : 0,

                // نسب المديونية (Leverage Ratios)
                DebtToAssets = balanceSheet.TotalAssets > 0 ? (balanceSheet.TotalLiabilities / balanceSheet.TotalAssets) * 100 : 0,
                DebtToEquity = balanceSheet.TotalEquity > 0 ? (balanceSheet.TotalLiabilities / balanceSheet.TotalEquity) * 100 : 0,

                // نسب الكفاءة (Efficiency Ratios)
                AssetTurnover = balanceSheet.TotalAssets > 0 ? incomeStatement.TotalRevenue / balanceSheet.TotalAssets : 0,
                InventoryTurnover = balanceSheet.Inventory > 0 ? incomeStatement.CostOfGoodsSold / balanceSheet.Inventory : 0
            };

            return ratios;
        }

        #endregion
    }

    #region Data Models

    public class IncomeStatementData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }

        // الإيرادات
        public decimal SalesRevenue { get; set; }
        public decimal OtherRevenue { get; set; }
        public decimal TotalRevenue { get; set; }

        // تكلفة المبيعات
        public decimal CostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }

        // المصروفات التشغيلية
        public decimal SalariesExpense { get; set; }
        public decimal DepreciationExpense { get; set; }
        public decimal UtilitiesExpense { get; set; }
        public decimal MaintenanceExpense { get; set; }
        public decimal OtherOperatingExpenses { get; set; }
        public decimal TotalOperatingExpenses { get; set; }

        // الربح التشغيلي
        public decimal OperatingProfit { get; set; }
        public decimal OperatingProfitMargin { get; set; }

        // إيرادات/مصروفات أخرى
        public decimal InterestIncome { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal OtherExpenses { get; set; }

        // صافي الربح
        public decimal NetProfitBeforeTax { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetProfitMargin { get; set; }
    }

    public class BalanceSheetData
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }

        // الأصول المتداولة
        public decimal Cash { get; set; }
        public decimal AccountsReceivable { get; set; }
        public decimal Inventory { get; set; }
        public decimal OtherCurrentAssets { get; set; }
        public decimal TotalCurrentAssets { get; set; }

        // الأصول الثابتة
        public decimal FixedAssetsGross { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal FixedAssetsNet { get; set; }

        // إجمالي الأصول
        public decimal TotalAssets { get; set; }

        // الخصوم المتداولة
        public decimal AccountsPayable { get; set; }
        public decimal SalariesPayable { get; set; }
        public decimal TaxesPayable { get; set; }
        public decimal OtherCurrentLiabilities { get; set; }
        public decimal TotalCurrentLiabilities { get; set; }

        // الخصوم طويلة الأجل
        public decimal LongTermLoans { get; set; }
        public decimal OtherLongTermLiabilities { get; set; }
        public decimal TotalLongTermLiabilities { get; set; }

        // إجمالي الخصوم
        public decimal TotalLiabilities { get; set; }

        // حقوق الملكية
        public decimal Capital { get; set; }
        public decimal RetainedEarnings { get; set; }
        public decimal CurrentYearProfit { get; set; }
        public decimal TotalEquity { get; set; }

        // إجمالي الخصوم وحقوق الملكية
        public decimal TotalLiabilitiesAndEquity { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class CashFlowData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }

        // الأنشطة التشغيلية
        public decimal NetProfit { get; set; }
        public decimal DepreciationAddBack { get; set; }
        public decimal ChangeInReceivables { get; set; }
        public decimal ChangeInInventory { get; set; }
        public decimal ChangeInPayables { get; set; }
        public decimal NetCashFromOperating { get; set; }

        // الأنشطة الاستثمارية
        public decimal PurchaseOfFixedAssets { get; set; }
        public decimal SaleOfFixedAssets { get; set; }
        public decimal NetCashFromInvesting { get; set; }

        // الأنشطة التمويلية
        public decimal CapitalContributions { get; set; }
        public decimal LoanProceeds { get; set; }
        public decimal LoanRepayments { get; set; }
        public decimal Dividends { get; set; }
        public decimal NetCashFromFinancing { get; set; }

        // صافي التغير في النقدية
        public decimal NetCashChange { get; set; }
        public decimal BeginningCash { get; set; }
        public decimal EndingCash { get; set; }
    }

    public class FinancialRatios
    {
        // نسب الربحية
        public decimal GrossProfitMargin { get; set; }
        public decimal OperatingProfitMargin { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal ROA { get; set; } // العائد على الأصول
        public decimal ROE { get; set; } // العائد على حقوق الملكية

        // نسب السيولة
        public decimal CurrentRatio { get; set; }
        public decimal QuickRatio { get; set; }

        // نسب المديونية
        public decimal DebtToAssets { get; set; }
        public decimal DebtToEquity { get; set; }

        // نسب الكفاءة
        public decimal AssetTurnover { get; set; }
        public decimal InventoryTurnover { get; set; }
    }

    #endregion

    #region Trial Balance Data Model

    /// <summary>
    /// نموذج بيانات ميزان المراجعة
    /// </summary>
    public class TrialBalanceData
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<TrialBalanceAccountData> Accounts { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
        public decimal Difference => Math.Abs(TotalDebit - TotalCredit);
    }

    /// <summary>
    /// نموذج بيانات حساب في ميزان المراجعة
    /// </summary>
    public class TrialBalanceAccountData
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string AccountTypeArabic { get; set; } = string.Empty;
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
        public int SortOrder { get; set; }
    }

    #endregion
}

