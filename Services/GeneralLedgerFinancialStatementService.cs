using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

/// <summary>Builds the three primary statements exclusively from posted general-ledger lines.</summary>
public sealed class GeneralLedgerFinancialStatementService
{
    private const string OpeningSource = "OpeningBalance";
    private const string ClosingSource = "YearEndClosing";
    private readonly FishFarmContext _context;

    public GeneralLedgerFinancialStatementService(FishFarmContext context) => _context = context;

    public async Task<IncomeStatementData> GenerateIncomeStatementAsync(DateTime startDate, DateTime endDate)
    {
        ValidateRange(startDate, endDate);
        var movements = await LoadMovementsAsync(startDate.Date, endDate.Date);
        return BuildIncomeStatement(startDate.Date, endDate.Date, ExcludeTransferJournals(movements));
    }

    public async Task<BalanceSheetData> GenerateBalanceSheetAsync(DateTime asOfDate)
    {
        var date = asOfDate.Date;
        var windowStart = await GetFiscalWindowStartAsync(date);
        var movements = await LoadMovementsAsync(windowStart, date);
        var balances = Aggregate(movements);
        decimal Asset(FinancialStatementCategory category) =>
            balances.Where(item => item.Type == LedgerAccountType.Asset && item.Category == category).Sum(item => item.NetDebit);
        decimal Liability(FinancialStatementCategory category) =>
            -balances.Where(item => item.Type == LedgerAccountType.Liability && item.Category == category).Sum(item => item.NetDebit);
        decimal Equity(FinancialStatementCategory category) =>
            -balances.Where(item => item.Type == LedgerAccountType.Equity && item.Category == category).Sum(item => item.NetDebit);

        var data = new BalanceSheetData
        {
            AsOfDate = date,
            GeneratedDate = DateTime.Now,
            Cash = Asset(FinancialStatementCategory.Cash),
            AccountsReceivable = Asset(FinancialStatementCategory.AccountsReceivable),
            Inventory = Asset(FinancialStatementCategory.Inventory),
            OtherCurrentAssets = Asset(FinancialStatementCategory.OtherCurrentAsset)
                + balances.Where(item => item.Type == LedgerAccountType.Asset
                    && item.Category == FinancialStatementCategory.Unclassified).Sum(item => item.NetDebit),
            FixedAssetsGross = Asset(FinancialStatementCategory.FixedAssetCost),
            AccumulatedDepreciation = -Asset(FinancialStatementCategory.AccumulatedDepreciation),
            OtherNonCurrentAssets = Asset(FinancialStatementCategory.OtherNonCurrentAsset),
            AccountsPayable = Liability(FinancialStatementCategory.AccountsPayable),
            SalariesPayable = Liability(FinancialStatementCategory.SalariesPayable),
            TaxesPayable = Liability(FinancialStatementCategory.TaxesPayable),
            OtherCurrentLiabilities = Liability(FinancialStatementCategory.OtherCurrentLiability)
                - balances.Where(item => item.Type == LedgerAccountType.Liability
                    && item.Category == FinancialStatementCategory.Unclassified).Sum(item => item.NetDebit),
            LongTermLoans = Liability(FinancialStatementCategory.LongTermLoan),
            OtherLongTermLiabilities = Liability(FinancialStatementCategory.OtherLongTermLiability),
            Capital = Equity(FinancialStatementCategory.Capital),
            RetainedEarnings = Equity(FinancialStatementCategory.RetainedEarnings),
            OtherEquity = Equity(FinancialStatementCategory.OtherEquity)
                - balances.Where(item => item.Type == LedgerAccountType.Equity
                    && item.Category == FinancialStatementCategory.Unclassified).Sum(item => item.NetDebit)
        };
        data.TotalCurrentAssets = data.Cash + data.AccountsReceivable + data.Inventory + data.OtherCurrentAssets;
        data.FixedAssetsNet = data.FixedAssetsGross - data.AccumulatedDepreciation;
        data.TotalNonCurrentAssets = data.FixedAssetsNet + data.OtherNonCurrentAssets;
        data.TotalAssets = data.TotalCurrentAssets + data.TotalNonCurrentAssets;
        data.TotalCurrentLiabilities = data.AccountsPayable + data.SalariesPayable + data.TaxesPayable + data.OtherCurrentLiabilities;
        data.TotalLongTermLiabilities = data.LongTermLoans + data.OtherLongTermLiabilities;
        data.TotalLiabilities = data.TotalCurrentLiabilities + data.TotalLongTermLiabilities;
        var closingPosted = await HasPostedClosingJournalAsOfAsync(date);
        data.CurrentYearProfit = closingPosted
            ? 0m
            : BuildIncomeStatement(windowStart, date, ExcludeTransferJournals(movements)).NetProfit;
        data.TotalEquity = data.Capital + data.RetainedEarnings + data.OtherEquity + data.CurrentYearProfit;
        data.TotalLiabilitiesAndEquity = data.TotalLiabilities + data.TotalEquity;
        data.IsBalanced = Math.Abs(data.TotalAssets - data.TotalLiabilitiesAndEquity) <= 0.01m;
        return data;
    }

    public async Task<CashFlowData> GenerateCashFlowStatementAsync(DateTime startDate, DateTime endDate)
    {
        ValidateRange(startDate, endDate);
        var start = startDate.Date;
        var end = endDate.Date;
        var movements = await LoadMovementsAsync(start, end);
        var income = BuildIncomeStatement(start, end, ExcludeTransferJournals(movements));
        decimal operatingCash = 0m, purchases = 0m, sales = 0m, capital = 0m, borrowings = 0m, repayments = 0m, dividends = 0m;

        foreach (var entry in ExcludeTransferJournals(movements).GroupBy(item => item.EntryId))
        {
            var cashDelta = entry.Where(item => item.Category == FinancialStatementCategory.Cash)
                .Sum(item => item.Debit - item.Credit);
            if (cashDelta == 0m) continue;
            var counterparts = entry.Where(item => item.Category != FinancialStatementCategory.Cash).ToArray();
            var investing = counterparts.Any(item => item.Category is FinancialStatementCategory.FixedAssetCost
                or FinancialStatementCategory.AccumulatedDepreciation or FinancialStatementCategory.OtherNonCurrentAsset);
            var financing = counterparts.Any(item => item.Type == LedgerAccountType.Equity
                || item.Category is FinancialStatementCategory.LongTermLoan or FinancialStatementCategory.OtherLongTermLiability);
            if (investing)
            {
                if (cashDelta < 0m) purchases += cashDelta;
                else sales += cashDelta;
            }
            else if (financing)
            {
                var equityCounterpart = counterparts.Any(item => item.Type == LedgerAccountType.Equity);
                if (cashDelta > 0m)
                {
                    if (equityCounterpart) capital += cashDelta;
                    else borrowings += cashDelta;
                }
                else if (equityCounterpart) dividends += -cashDelta;
                else repayments += -cashDelta;
            }
            else operatingCash += cashDelta;
        }

        var beginningAr = await GetCategoryBalanceAtPeriodStartAsync(start, LedgerAccountType.Asset, FinancialStatementCategory.AccountsReceivable);
        var endingAr = await GetCategoryBalanceAsOfAsync(end, LedgerAccountType.Asset, FinancialStatementCategory.AccountsReceivable);
        var beginningInventory = await GetCategoryBalanceAtPeriodStartAsync(start, LedgerAccountType.Asset, FinancialStatementCategory.Inventory);
        var endingInventory = await GetCategoryBalanceAsOfAsync(end, LedgerAccountType.Asset, FinancialStatementCategory.Inventory);
        var beginningAp = await GetCategoryBalanceAtPeriodStartAsync(start, LedgerAccountType.Liability, FinancialStatementCategory.AccountsPayable);
        var endingAp = await GetCategoryBalanceAsOfAsync(end, LedgerAccountType.Liability, FinancialStatementCategory.AccountsPayable);
        var data = new CashFlowData
        {
            StartDate = start,
            EndDate = end,
            GeneratedDate = DateTime.Now,
            NetProfit = income.NetProfit,
            DepreciationAddBack = income.DepreciationExpense,
            ChangeInReceivables = -(endingAr - beginningAr),
            ChangeInInventory = -(endingInventory - beginningInventory),
            ChangeInPayables = endingAp - beginningAp,
            PurchaseOfFixedAssets = purchases,
            SaleOfFixedAssets = sales,
            CapitalContributions = capital,
            LoanProceeds = borrowings,
            LoanRepayments = repayments,
            Dividends = dividends,
            BeginningCash = await GetCategoryBalanceAtPeriodStartAsync(start, LedgerAccountType.Asset, FinancialStatementCategory.Cash),
            EndingCash = await GetCategoryBalanceAsOfAsync(end, LedgerAccountType.Asset, FinancialStatementCategory.Cash)
        };
        var explainedOperating = data.NetProfit + data.DepreciationAddBack + data.ChangeInReceivables
            + data.ChangeInInventory + data.ChangeInPayables;
        data.OtherOperatingAdjustments = operatingCash - explainedOperating;
        data.NetCashFromOperating = operatingCash;
        data.NetCashFromInvesting = data.PurchaseOfFixedAssets + data.SaleOfFixedAssets;
        data.NetCashFromFinancing = data.CapitalContributions + data.LoanProceeds - data.LoanRepayments - data.Dividends;
        data.NetCashChange = data.NetCashFromOperating + data.NetCashFromInvesting + data.NetCashFromFinancing;
        data.IsReconciled = Math.Abs(data.EndingCash - data.BeginningCash - data.NetCashChange) <= 0.01m;
        return data;
    }

    public async Task<FinancialRatios> CalculateFinancialRatiosAsync(DateTime asOfDate)
    {
        var balance = await GenerateBalanceSheetAsync(asOfDate);
        var income = await GenerateIncomeStatementAsync(await GetFiscalWindowStartAsync(asOfDate.Date), asOfDate.Date);
        return new FinancialRatios
        {
            GrossProfitMargin = income.GrossProfitMargin,
            OperatingProfitMargin = income.OperatingProfitMargin,
            NetProfitMargin = income.NetProfitMargin,
            ROA = Ratio(income.NetProfit, balance.TotalAssets),
            ROE = Ratio(income.NetProfit, balance.TotalEquity),
            CurrentRatio = balance.TotalCurrentLiabilities == 0 ? 0 : balance.TotalCurrentAssets / balance.TotalCurrentLiabilities,
            QuickRatio = balance.TotalCurrentLiabilities == 0 ? 0 : (balance.TotalCurrentAssets - balance.Inventory) / balance.TotalCurrentLiabilities,
            DebtToAssets = Ratio(balance.TotalLiabilities, balance.TotalAssets),
            DebtToEquity = Ratio(balance.TotalLiabilities, balance.TotalEquity),
            AssetTurnover = balance.TotalAssets == 0 ? 0 : income.TotalRevenue / balance.TotalAssets,
            InventoryTurnover = balance.Inventory == 0 ? 0 : income.CostOfGoodsSold / balance.Inventory
        };
    }

    private static IncomeStatementData BuildIncomeStatement(DateTime start, DateTime end, IReadOnlyCollection<LedgerMovement> movements)
    {
        decimal Revenue(FinancialStatementCategory category) => movements.Where(item => item.Type == LedgerAccountType.Revenue
            && item.Category == category).Sum(item => item.Credit - item.Debit);
        decimal Expense(FinancialStatementCategory category) => movements.Where(item => item.Type == LedgerAccountType.Expense
            && item.Category == category).Sum(item => item.Debit - item.Credit);
        var data = new IncomeStatementData
        {
            StartDate = start,
            EndDate = end,
            GeneratedDate = DateTime.Now,
            SalesRevenue = Revenue(FinancialStatementCategory.SalesRevenue),
            OtherRevenue = Revenue(FinancialStatementCategory.OtherRevenue)
                + movements.Where(item => item.Type == LedgerAccountType.Revenue && item.Category == FinancialStatementCategory.Unclassified).Sum(item => item.Credit - item.Debit),
            CostOfGoodsSold = Expense(FinancialStatementCategory.CostOfGoodsSold),
            SalariesExpense = Expense(FinancialStatementCategory.SalariesExpense),
            DepreciationExpense = Expense(FinancialStatementCategory.DepreciationExpense),
            UtilitiesExpense = Expense(FinancialStatementCategory.UtilitiesExpense),
            MaintenanceExpense = Expense(FinancialStatementCategory.MaintenanceExpense),
            OtherOperatingExpenses = Expense(FinancialStatementCategory.OtherOperatingExpense)
                + movements.Where(item => item.Type == LedgerAccountType.Expense && item.Category == FinancialStatementCategory.Unclassified).Sum(item => item.Debit - item.Credit),
            InterestIncome = Revenue(FinancialStatementCategory.InterestIncome),
            InterestExpense = Expense(FinancialStatementCategory.InterestExpense),
            IncomeTax = Expense(FinancialStatementCategory.IncomeTaxExpense)
        };
        data.TotalRevenue = data.SalesRevenue + data.OtherRevenue;
        data.GrossProfit = data.TotalRevenue - data.CostOfGoodsSold;
        data.GrossProfitMargin = Ratio(data.GrossProfit, data.TotalRevenue);
        data.TotalOperatingExpenses = data.SalariesExpense + data.DepreciationExpense + data.UtilitiesExpense
            + data.MaintenanceExpense + data.OtherOperatingExpenses;
        data.OperatingProfit = data.GrossProfit - data.TotalOperatingExpenses;
        data.OperatingProfitMargin = Ratio(data.OperatingProfit, data.TotalRevenue);
        data.NetProfitBeforeTax = data.OperatingProfit + data.InterestIncome + data.OtherIncome - data.InterestExpense - data.OtherExpenses;
        data.NetProfit = data.NetProfitBeforeTax - data.IncomeTax;
        data.NetProfitMargin = Ratio(data.NetProfit, data.TotalRevenue);
        return data;
    }

    private async Task<LedgerMovement[]> LoadMovementsAsync(DateTime fromDate, DateTime toDate) =>
        (await _context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntry.EntryDate >= fromDate && line.JournalEntry.EntryDate <= toDate
                && (line.JournalEntry.Status == JournalEntryStatus.Posted || line.JournalEntry.Status == JournalEntryStatus.Reversed))
            .Select(line => new LedgerMovement(line.JournalEntryId, line.JournalEntry.EntryDate, line.JournalEntry.Source,
                line.LedgerAccount.Type, line.LedgerAccount.FinancialStatementCategory, line.Debit, line.Credit))
            .ToListAsync()).ToArray();

    private async Task<DateTime> GetFiscalWindowStartAsync(DateTime date) =>
        await _context.FiscalYears.AsNoTracking().Where(item => item.StartDate <= date && item.EndDate >= date)
            .Select(item => (DateTime?)item.StartDate).SingleOrDefaultAsync() ?? new DateTime(date.Year, 1, 1);

    private async Task<bool> HasPostedClosingJournalAsOfAsync(DateTime date)
    {
        var closingEntryId = await _context.FiscalYears.AsNoTracking()
            .Where(item => item.StartDate <= date && item.EndDate >= date)
            .Select(item => item.ClosingJournalEntryId)
            .SingleOrDefaultAsync();
        return closingEntryId.HasValue && await _context.JournalEntries.AsNoTracking()
            .AnyAsync(item => item.Id == closingEntryId.Value
                && item.Status == JournalEntryStatus.Posted && item.EntryDate <= date);
    }

    private async Task<decimal> GetCategoryBalanceAsOfAsync(DateTime date, LedgerAccountType type, FinancialStatementCategory category)
    {
        var movements = await LoadMovementsAsync(await GetFiscalWindowStartAsync(date), date);
        var net = movements.Where(item => item.Type == type && item.Category == category).Sum(item => item.Debit - item.Credit);
        return type == LedgerAccountType.Asset ? net : -net;
    }

    private async Task<decimal> GetCategoryBalanceAtPeriodStartAsync(DateTime start, LedgerAccountType type, FinancialStatementCategory category)
    {
        var movements = await LoadMovementsAsync(await GetFiscalWindowStartAsync(start), start);
        var net = movements.Where(item => item.Type == type && item.Category == category
            && (item.EntryDate < start || item.Source == OpeningSource)).Sum(item => item.Debit - item.Credit);
        return type == LedgerAccountType.Asset ? net : -net;
    }

    private static LedgerMovement[] ExcludeTransferJournals(IEnumerable<LedgerMovement> movements) =>
        movements.Where(item => item.Source != OpeningSource && item.Source != ClosingSource).ToArray();

    private static AccountBalance[] Aggregate(IEnumerable<LedgerMovement> movements) => movements
        .GroupBy(item => new { item.Type, item.Category })
        .Select(group => new AccountBalance(group.Key.Type, group.Key.Category, group.Sum(item => item.Debit - item.Credit))).ToArray();

    private static decimal Ratio(decimal numerator, decimal denominator) => denominator == 0m ? 0m : numerator / denominator * 100m;
    private static void ValidateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
            throw new ArgumentException("The report end date must not precede its start date.", nameof(endDate));
    }

    private sealed record LedgerMovement(long EntryId, DateTime EntryDate, string Source, LedgerAccountType Type,
        FinancialStatementCategory Category, decimal Debit, decimal Credit);
    private sealed record AccountBalance(LedgerAccountType Type, FinancialStatementCategory Category, decimal NetDebit);
}
