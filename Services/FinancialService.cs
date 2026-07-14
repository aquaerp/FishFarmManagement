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
    /// General-ledger based; subject to external accounting review.
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
            => await new GeneralLedgerFinancialStatementService(_context)
                .GenerateIncomeStatementAsync(startDate, endDate);

        #endregion

        #region Balance Sheet (الميزانية العمومية)

        /// <summary>
        /// إنشاء الميزانية العمومية في تاريخ محدد
        /// </summary>
        public async Task<BalanceSheetData> GenerateBalanceSheetAsync(DateTime asOfDate)
            => await new GeneralLedgerFinancialStatementService(_context)
                .GenerateBalanceSheetAsync(asOfDate);

        #endregion

        #region Cash Flow (التدفقات النقدية)

        /// <summary>
        /// إنشاء قائمة التدفقات النقدية
        /// </summary>
        public async Task<CashFlowData> GenerateCashFlowStatementAsync(DateTime startDate, DateTime endDate)
            => await new GeneralLedgerFinancialStatementService(_context)
                .GenerateCashFlowStatementAsync(startDate, endDate);

        #endregion

        #region Financial Ratios (النسب المالية)

        /// <summary>
        /// حساب النسب المالية
        /// </summary>
        public async Task<FinancialRatios> CalculateFinancialRatiosAsync(DateTime asOfDate)
            => await new GeneralLedgerFinancialStatementService(_context)
                .CalculateFinancialRatiosAsync(asOfDate);

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
        public decimal OtherNonCurrentAssets { get; set; }
        public decimal TotalNonCurrentAssets { get; set; }

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
        public decimal OtherEquity { get; set; }
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
        public decimal OtherOperatingAdjustments { get; set; }
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
        public bool IsReconciled { get; set; }
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
