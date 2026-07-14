using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Phase2GeneralLedgerTests
{
    [Fact]
    public void BalanceValidator_RejectsUnbalancedEntry()
    {
        var lines = new[]
        {
            new JournalLineRequest(1, 100m, 0m),
            new JournalLineRequest(2, 0m, 99.99m)
        };

        Assert.Throws<InvalidOperationException>(() => JournalBalanceValidator.Validate(lines));
    }

    [Fact]
    public void BalanceValidator_RejectsAmountsBeyondHalalaPrecision()
    {
        var lines = new[]
        {
            new JournalLineRequest(1, 10.001m, 0m),
            new JournalLineRequest(2, 0m, 10.001m)
        };

        Assert.Throws<InvalidOperationException>(() => JournalBalanceValidator.Validate(lines));
    }

    [Fact]
    public void BalanceValidator_AcceptsOneMillionBalancedGeneratedLines()
    {
        var lines = Enumerable.Range(0, 1_000_000)
            .Select(index => index % 2 == 0
                ? new JournalLineRequest(1, 0.01m, 0m)
                : new JournalLineRequest(2, 0m, 0.01m));

        var totals = JournalBalanceValidator.Validate(lines);

        Assert.Equal(5_000m, totals.Debit);
        Assert.Equal(5_000m, totals.Credit);
    }

    [Fact]
    public void GeneralLedger_RejectsForeignCurrencyAccountsUntilFxAccountingExists()
    {
        using var database = LedgerTestDatabase.Create();
        var foreignAccounts = database.SeedForeignCurrencyAccounts();
        var request = database.BalancedRequest(25m) with
        {
            Lines = new[]
            {
                new JournalLineRequest(foreignAccounts.DebitId, 25m, 0m),
                new JournalLineRequest(foreignAccounts.CreditId, 0m, 25m)
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            new GeneralLedgerService(database.Context).CreateDraft(request, "fx-user", "Unsupported USD journal"));
    }

    [Fact]
    public void AccountCurrency_IsImmutableAtDatabaseLevelAfterJournalUse()
    {
        using var database = LedgerTestDatabase.Create();
        new GeneralLedgerService(database.Context).CreateDraft(
            database.BalancedRequest(10m), "creator", "Establish account usage");

        database.ChangeUsedDebitAccountCurrency("USD");

        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public async Task CrossYearReversal_UsesNextYearPeriodAndKeepsReportsSeparated()
    {
        using var database = LedgerTestDatabase.Create();
        var ledger = new GeneralLedgerService(database.Context);
        var original = ledger.CreateDraft(database.BalancedRequest(90m), "creator", "2026 revenue");
        ledger.Approve(original.Id, "approver", "2026 revenue checked");
        ledger.Post(original.Id, "poster", "2026 revenue posted");
        var nextPeriodId = database.AddNextYearJanuaryPeriod();

        Assert.Throws<InvalidOperationException>(() => ledger.CreateDraft(
            database.BalancedRequest(1m) with { EntryDate = new DateTime(2027, 1, 2) },
            "creator", "Wrong period date"));

        var reversal = ledger.Reverse(original.Id, new DateTime(2027, 1, 2), "controller", "Reverse in next year");
        var reports = new FinancialService(database.Context);
        var report2026 = await reports.GenerateIncomeStatementAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        var report2027 = await reports.GenerateIncomeStatementAsync(
            new DateTime(2027, 1, 1), new DateTime(2027, 1, 31));

        Assert.Equal(nextPeriodId, reversal.FiscalPeriodId);
        Assert.Equal(90m, report2026.OtherRevenue);
        Assert.Equal(-90m, report2027.OtherRevenue);
    }

    [Fact]
    public void JournalLifecycle_RequiresIndependentApprovalAndPersistsAudit()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new GeneralLedgerService(database.Context);
        var draft = service.CreateDraft(database.BalancedRequest(125.55m), "creator", "Monthly sale");

        Assert.Equal("JE-0000000001", draft.EntryNumber);
        Assert.Throws<InvalidOperationException>(() => service.Approve(draft.Id, "creator", "Self approval"));
        service.Approve(draft.Id, "approver", "Supporting documents checked");
        var posted = service.Post(draft.Id, "poster", "Approved for posting");

        Assert.Equal(JournalEntryStatus.Posted, posted.Status);
        Assert.Equal(3, database.Context.AccountingAuditEvents.Count(item => item.EntityId == draft.Id.ToString()));
        Assert.All(database.Context.AccountingAuditEvents, item => Assert.False(string.IsNullOrWhiteSpace(item.Reason)));
    }

    [Fact]
    public void ClosedPeriod_RejectsNewJournalEntries()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new GeneralLedgerService(database.Context);
        service.ClosePeriod(database.PeriodId, "controller", "Month end completed");

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateDraft(database.BalancedRequest(10m), "creator", "Should fail"));
    }

    [Fact]
    public void ClosedFiscalYear_RejectsPostingAnExistingApprovedEntry()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new GeneralLedgerService(database.Context);
        var draft = service.CreateDraft(database.BalancedRequest(10m), "creator", "Year close test");
        service.Approve(draft.Id, "approver", "Approved before year close");
        var year = database.Context.FiscalYears.Single();
        year.IsClosed = true;
        database.Context.SaveChanges();

        Assert.Throws<InvalidOperationException>(() => service.Post(draft.Id, "poster", "Should fail"));
    }

    [Fact]
    public void PostedEntry_IsImmutableAtDatabaseLevel()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new GeneralLedgerService(database.Context);
        var draft = service.CreateDraft(database.BalancedRequest(50m), "creator", "Immutability proof");
        service.Approve(draft.Id, "approver", "Checked");
        service.Post(draft.Id, "poster", "Posted");

        var line = database.Context.JournalEntryLines.First(item => item.JournalEntryId == draft.Id);
        line.Debit = 60m;

        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void Reversal_SwapsDebitAndCreditAndRetainsOriginal()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new GeneralLedgerService(database.Context);
        var draft = service.CreateDraft(database.BalancedRequest(75m), "creator", "Original entry");
        service.Approve(draft.Id, "approver", "Checked");
        service.Post(draft.Id, "poster", "Posted");

        var reversal = service.Reverse(draft.Id, new DateTime(2026, 1, 20), "controller", "Correction required");
        var original = database.Context.JournalEntries.AsNoTracking().Single(item => item.Id == draft.Id);
        var reversalLines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(item => item.JournalEntryId == reversal.Id).OrderBy(item => item.LineNumber).ToArray();

        Assert.Equal(JournalEntryStatus.Reversed, original.Status);
        Assert.Equal(JournalEntryStatus.Posted, reversal.Status);
        Assert.Equal(draft.Id, reversal.ReversalOfJournalEntryId);
        Assert.Equal(75m, reversalLines[0].Credit);
        Assert.Equal(75m, reversalLines[1].Debit);
        Assert.Equal("JE-0000000002", reversal.EntryNumber);
    }

    [Fact]
    public void Migration_CreatesGeneralLedgerSchemaAndImmutabilityTriggers()
    {
        using var database = LedgerTestDatabase.Create();
        using var command = database.Context.Database.GetDbConnection().CreateCommand();
        database.Context.Database.OpenConnection();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('LedgerAccounts','FiscalPeriods','JournalEntries','JournalEntryLines','AccountingAuditEvents');";
        Assert.Equal(5L, Convert.ToInt64(command.ExecuteScalar()));
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='trigger' AND name LIKE 'TRG_Journal%';";
        Assert.Equal(7L, Convert.ToInt64(command.ExecuteScalar()));
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='trigger' AND name IN ('TRG_JournalEntryLines_SarOnlyInsert','TRG_JournalEntryLines_SarOnlyUpdate','TRG_LedgerAccounts_CurrencyImmutableAfterUse');";
        Assert.Equal(3L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public void OneHundredOperationalScenarios_PostAndTrialBalanceMatchesLedger()
    {
        using var database = LedgerTestDatabase.Create();
        var ledger = new GeneralLedgerService(database.Context);
        var sources = new[] { "Sales", "Purchasing", "Payroll", "Inventory", "Depreciation", "Returns" };

        for (var index = 1; index <= 100; index++)
        {
            var baseRequest = database.BalancedRequest(index + 0.25m);
            var request = baseRequest with
            {
                Description = $"Scenario {index}",
                Source = sources[(index - 1) % sources.Length],
                Reference = $"SCN-{index:D3}"
            };
            var draft = ledger.CreateDraft(request, $"creator-{index % 3}", "Generated G2 scenario");
            ledger.Approve(draft.Id, "independent-approver", "Scenario approved");
            ledger.Post(draft.Id, "posting-controller", "Scenario posted");
        }

        var report = new GeneralLedgerReportingService(database.Context)
            .GetTrialBalance(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var ledgerTotals = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntry.Status == JournalEntryStatus.Posted)
            .AsEnumerable();

        Assert.Equal(100, database.Context.JournalEntries.Count(entry => entry.Status == JournalEntryStatus.Posted));
        Assert.Equal(100, database.Context.JournalEntries.Select(entry => entry.EntryNumber).Distinct().Count());
        Assert.True(report.IsBalanced);
        Assert.Equal(ledgerTotals.Sum(line => line.Debit), report.TotalDebit);
        Assert.Equal(ledgerTotals.Sum(line => line.Credit), report.TotalCredit);
    }

    [Fact]
    public void OperationalPosting_RejectsUnapprovedPostingConfiguration()
    {
        using var database = LedgerTestDatabase.Create();
        new AccountingConfigurationService(database.Context).CreatePilotDraft("configuration-owner");
        var orderId = database.SeedCompletedSalesOrder(115m, 15m);

        Assert.Throws<InvalidOperationException>(() =>
            new OperationalPostingService(database.Context).CreateSalesCompletionDraft(
                orderId, database.PeriodId, "accounting-user", "Transfer completed sale"));
    }

    [Fact]
    public void SalesPosting_UsesApprovedMapAndCannotBeTransferredTwice()
    {
        using var database = LedgerTestDatabase.Create();
        var configurationService = new AccountingConfigurationService(database.Context);
        var configuration = configurationService.CreatePilotDraft("configuration-owner");
        configurationService.Approve(configuration.Id, "independent-accountant", "Pilot map reviewed");
        var orderId = database.SeedCompletedSalesOrder(115m, 15m);
        var posting = new OperationalPostingService(database.Context);

        var entry = posting.CreateSalesCompletionDraft(
            orderId, database.PeriodId, "accounting-user", "Transfer completed sale");
        var lines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntryId == entry.Id)
            .Select(line => new { line.LedgerAccount.Code, line.Debit, line.Credit })
            .OrderBy(line => line.Code).ToArray();

        Assert.Equal(JournalEntryStatus.Draft, entry.Status);
        Assert.Contains(lines, line => line.Code == "1120" && line.Debit == 115m);
        Assert.Contains(lines, line => line.Code == "4100" && line.Credit == 100m);
        Assert.Contains(lines, line => line.Code == "2200" && line.Credit == 15m);
        Assert.Single(database.Context.OperationalPostingRecords);
        Assert.Throws<InvalidOperationException>(() => posting.CreateSalesCompletionDraft(
            orderId, database.PeriodId, "accounting-user", "Duplicate transfer"));
        Assert.Single(database.Context.JournalEntries);
    }

    [Fact]
    public void PurchasePosting_SeparatesRecoverableVatAndPayable()
    {
        using var database = LedgerTestDatabase.Create();
        var configurationService = new AccountingConfigurationService(database.Context);
        var configuration = configurationService.CreatePilotDraft("configuration-owner");
        configurationService.Approve(configuration.Id, "independent-accountant", "Pilot map reviewed");
        var receivingId = database.SeedApprovedPurchaseReceiving(230m, 30m);

        var entry = new OperationalPostingService(database.Context).CreatePurchaseReceiptDraft(
            receivingId, database.PeriodId, "accounting-user", "Transfer received purchase");
        var lines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntryId == entry.Id)
            .Select(line => new { line.LedgerAccount.Code, line.Debit, line.Credit })
            .ToArray();

        Assert.Contains(lines, line => line.Code == "1130" && line.Debit == 200m);
        Assert.Contains(lines, line => line.Code == "1140" && line.Debit == 30m);
        Assert.Contains(lines, line => line.Code == "2100" && line.Credit == 230m);
        Assert.Equal(lines.Sum(line => line.Debit), lines.Sum(line => line.Credit));
    }

    [Fact]
    public void ApprovedPostingMap_IsImmutableAtDatabaseLevel()
    {
        using var database = LedgerTestDatabase.Create();
        var configurationService = new AccountingConfigurationService(database.Context);
        var configuration = configurationService.CreatePilotDraft("configuration-owner");
        configurationService.Approve(configuration.Id, "independent-accountant", "Pilot map reviewed");
        var mapping = database.Context.PostingMappings.First();
        mapping.IsActive = false;

        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void CustomerAndSupplierPayments_PostOppositeCashEntries()
    {
        using var database = LedgerTestDatabase.Create();
        database.ApprovePilotConfiguration();
        var customerPaymentId = database.SeedCompletedCustomerPayment(80m);
        var supplierPaymentId = database.SeedCompletedSupplierPayment(60m);
        var posting = new OperationalPostingService(database.Context);

        var receipt = posting.CreateCustomerPaymentDraft(
            customerPaymentId, database.PeriodId, "cashier", "Customer receipt verified");
        var payment = posting.CreateSupplierPaymentDraft(
            supplierPaymentId, database.PeriodId, "cashier", "Supplier payment verified");

        var receiptLines = database.LinesWithAccountCodes(receipt.Id);
        var paymentLines = database.LinesWithAccountCodes(payment.Id);
        Assert.Contains(receiptLines, line => line.Code == "1110" && line.Debit == 80m);
        Assert.Contains(receiptLines, line => line.Code == "1120" && line.Credit == 80m);
        Assert.Contains(paymentLines, line => line.Code == "2100" && line.Debit == 60m);
        Assert.Contains(paymentLines, line => line.Code == "1110" && line.Credit == 60m);
    }

    [Fact]
    public void PayrollPosting_ReconcilesGrossNetAndDeductions()
    {
        using var database = LedgerTestDatabase.Create();
        database.ApprovePilotConfiguration();
        var salaryId = database.SeedApprovedSalary(1_000m, 850m, 150m);

        var entry = new OperationalPostingService(database.Context).CreatePayrollAccrualDraft(
            salaryId, database.PeriodId, "payroll-accountant", "Approved January payroll");
        var lines = database.LinesWithAccountCodes(entry.Id);

        Assert.Contains(lines, line => line.Code == "5200" && line.Debit == 1_000m);
        Assert.Contains(lines, line => line.Code == "2110" && line.Credit == 850m);
        Assert.Contains(lines, line => line.Code == "2120" && line.Credit == 150m);
        Assert.Equal(lines.Sum(line => line.Debit), lines.Sum(line => line.Credit));
    }

    [Fact]
    public void DepreciationPosting_DebitsExpenseAndCreditsAccumulatedDepreciation()
    {
        using var database = LedgerTestDatabase.Create();
        database.ApprovePilotConfiguration();
        var depreciationId = database.SeedApprovedDepreciation(125m);

        var entry = new OperationalPostingService(database.Context).CreateDepreciationDraft(
            depreciationId, database.PeriodId, "asset-accountant", "Approved January depreciation");
        var lines = database.LinesWithAccountCodes(entry.Id);

        Assert.Contains(lines, line => line.Code == "5300" && line.Debit == 125m);
        Assert.Contains(lines, line => line.Code == "1520" && line.Credit == 125m);
    }

    [Fact]
    public void OpeningBalances_AreLimitedToOneBalancedBalanceSheetJournalPerYear()
    {
        using var database = LedgerTestDatabase.Create();
        new AccountingConfigurationService(database.Context).CreatePilotDraft("configuration-owner");
        var cashId = database.AccountId("1110");
        var retainedEarningsId = database.AccountId("3200");
        var service = new FiscalYearClosingService(database.Context);

        var opening = service.CreateOpeningBalanceDraft(
            database.FiscalYearId,
            new[]
            {
                new OpeningBalanceRequest(cashId, 1_500m, 0m),
                new OpeningBalanceRequest(retainedEarningsId, 0m, 1_500m)
            },
            "opening-accountant",
            "Signed opening balance schedule");

        Assert.Equal(JournalEntryStatus.Draft, opening.Status);
        Assert.Equal("OpeningBalance", opening.Source);
        Assert.Equal(opening.Id, database.Context.FiscalYears.Single().OpeningBalanceJournalEntryId);
        Assert.Throws<InvalidOperationException>(() => service.CreateOpeningBalanceDraft(
            database.FiscalYearId,
            new[]
            {
                new OpeningBalanceRequest(cashId, 1m, 0m),
                new OpeningBalanceRequest(retainedEarningsId, 0m, 1m)
            },
            "opening-accountant",
            "Duplicate schedule"));
    }

    [Fact]
    public void FiscalYearClose_RequiresPostedClosingJournalAndMakesYearImmutable()
    {
        using var database = LedgerTestDatabase.Create();
        new AccountingConfigurationService(database.Context).CreatePilotDraft("configuration-owner");
        database.PostRevenue(400m);
        database.PrepareFinalPeriod();
        var service = new FiscalYearClosingService(database.Context);

        var closing = service.CreateYearEndClosingDraft(
            database.FiscalYearId,
            database.AccountId("3200"),
            "closing-accountant",
            "Year-end income statement review");
        var lines = database.LinesWithAccountCodes(closing.Id);

        Assert.Contains(lines, line => line.Code == "410100" && line.Debit == 400m);
        Assert.Contains(lines, line => line.Code == "3200" && line.Credit == 400m);
        Assert.Throws<InvalidOperationException>(() => service.CloseFiscalYear(
            database.FiscalYearId, "controller", "Closing journal is still a draft"));

        var ledger = new GeneralLedgerService(database.Context);
        ledger.Approve(closing.Id, "independent-approver", "Closing journal independently checked");
        ledger.Post(closing.Id, "posting-controller", "Closing journal posted");
        service.CloseFiscalYear(database.FiscalYearId, "financial-controller", "All year-end checks completed");

        var year = database.Context.FiscalYears.AsNoTracking().Include(item => item.Periods).Single();
        Assert.True(year.IsClosed);
        Assert.NotNull(year.ClosedAtUtc);
        Assert.All(year.Periods, period => Assert.Equal(FiscalPeriodStatus.Closed, period.Status));

        database.Context.ChangeTracker.Clear();
        var stored = database.Context.FiscalYears.Single();
        stored.Name = "Tampered year";
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public async Task ClosedFiscalYear_CreatesTwelvePeriodNextYearAndBalancedOpeningDraft()
    {
        using var database = LedgerTestDatabase.Create();
        new AccountingConfigurationService(database.Context).CreatePilotDraft("configuration-owner");
        database.PostRevenue(725m);
        database.PrepareFinalPeriod();
        var service = new FiscalYearClosingService(database.Context);
        var closing = service.CreateYearEndClosingDraft(
            database.FiscalYearId, database.AccountId("3200"), "closing-accountant", "Prepare year close");
        var ledger = new GeneralLedgerService(database.Context);
        ledger.Approve(closing.Id, "independent-approver", "Closing checked");
        ledger.Post(closing.Id, "posting-controller", "Closing posted");
        service.CloseFiscalYear(database.FiscalYearId, "financial-controller", "Year approved for close");

        var closedBalance = await new FinancialService(database.Context)
            .GenerateBalanceSheetAsync(new DateTime(2026, 12, 31));
        Assert.Equal(0m, closedBalance.CurrentYearProfit);
        Assert.True(closedBalance.IsBalanced);

        var result = service.CreateNextFiscalYearWithOpeningDraft(
            database.FiscalYearId,
            "FY2027",
            new DateTime(2027, 1, 1),
            new DateTime(2027, 12, 31),
            "opening-accountant",
            "Carry forward audited closing balances");
        var openingLines = database.LinesWithAccountCodes(result.OpeningBalanceDraft.Id);

        Assert.Equal(12, result.FiscalYear.Periods.Count);
        Assert.Equal(JournalEntryStatus.Draft, result.OpeningBalanceDraft.Status);
        Assert.Contains(openingLines, line => line.Code == "110100" && line.Debit == 725m);
        Assert.Contains(openingLines, line => line.Code == "3200" && line.Credit == 725m);
        Assert.Equal(openingLines.Sum(line => line.Debit), openingLines.Sum(line => line.Credit));
    }

    [Fact]
    public async Task FinancialStatements_UseOnlyPostedGeneralLedgerAndReconcileCash()
    {
        using var database = LedgerTestDatabase.Create();
        new AccountingConfigurationService(database.Context).CreatePilotDraft("configuration-owner");
        var year = new FiscalYearClosingService(database.Context);
        var ledger = new GeneralLedgerService(database.Context);
        var opening = year.CreateOpeningBalanceDraft(
            database.FiscalYearId,
            new[]
            {
                new OpeningBalanceRequest(database.AccountId("1110"), 1_000m, 0m),
                new OpeningBalanceRequest(database.AccountId("1130"), 200m, 0m),
                new OpeningBalanceRequest(database.AccountId("1510"), 500m, 0m),
                new OpeningBalanceRequest(database.AccountId("3100"), 0m, 1_700m)
            },
            "opening-accountant",
            "Audited opening schedule");
        ledger.Approve(opening.Id, "opening-approver", "Opening balances checked");
        ledger.Post(opening.Id, "posting-controller", "Opening balances posted");

        database.PostJournal("Cash sale", "Sales", new[]
        {
            ("1110", 115m, 0m),
            ("4100", 0m, 100m),
            ("2200", 0m, 15m)
        });
        database.PostJournal("Cost of sale", "Inventory", new[]
        {
            ("5100", 30m, 0m),
            ("1130", 0m, 30m)
        });
        database.PostJournal("Payroll accrual", "Payroll", new[]
        {
            ("5200", 40m, 0m),
            ("2110", 0m, 40m)
        });
        database.PostJournal("Depreciation", "Depreciation", new[]
        {
            ("5300", 20m, 0m),
            ("1520", 0m, 20m)
        });
        database.CreateDraftJournal("Unposted misleading sale", new[]
        {
            ("1110", 999m, 0m),
            ("4100", 0m, 999m)
        });
        database.SeedCompletedSalesOrder(9_999m, 0m); // Operational data alone must not affect statements.

        var service = new FinancialService(database.Context);
        var income = await service.GenerateIncomeStatementAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var balance = await service.GenerateBalanceSheetAsync(new DateTime(2026, 1, 31));
        var cashFlow = await service.GenerateCashFlowStatementAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(100m, income.SalesRevenue);
        Assert.Equal(30m, income.CostOfGoodsSold);
        Assert.Equal(40m, income.SalariesExpense);
        Assert.Equal(20m, income.DepreciationExpense);
        Assert.Equal(10m, income.NetProfit);
        Assert.Equal(1_115m, balance.Cash);
        Assert.Equal(1_765m, balance.TotalAssets);
        Assert.Equal(10m, balance.CurrentYearProfit);
        Assert.True(balance.IsBalanced);
        Assert.Equal(1_000m, cashFlow.BeginningCash);
        Assert.Equal(115m, cashFlow.NetCashFromOperating);
        Assert.Equal(1_115m, cashFlow.EndingCash);
        Assert.True(cashFlow.IsReconciled);
    }

    private sealed class LedgerTestDatabase : IDisposable
    {
        private readonly string _databasePath;
        public FishFarmContext Context { get; }
        public int PeriodId { get; private set; }
        public int FiscalYearId { get; private set; }
        private int DebitAccountId { get; set; }
        private int CreditAccountId { get; set; }

        private LedgerTestDatabase(string databasePath, FishFarmContext context)
        {
            _databasePath = databasePath;
            Context = context;
        }

        public static LedgerTestDatabase Create()
        {
            var path = Path.Combine(Path.GetTempPath(), $"aquafarm-ledger-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite($"Data Source={path};Pooling=False")
                .Options;
            var database = new LedgerTestDatabase(path, new FishFarmContext(options));
            database.Context.Database.Migrate();
            database.Seed();
            return database;
        }

        public JournalDraftRequest BalancedRequest(decimal amount) => new(
            new DateTime(2026, 1, 15),
            PeriodId,
            "Test journal entry",
            "Test",
            Guid.NewGuid().ToString("N"),
            new[]
            {
                new JournalLineRequest(DebitAccountId, amount, 0m, Description: "Debit"),
                new JournalLineRequest(CreditAccountId, 0m, amount, Description: "Credit")
            });

        private void Seed()
        {
            var year = new FiscalYear
            {
                Name = "FY2026",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            };
            var period = new FiscalPeriod
            {
                FiscalYear = year,
                Name = "2026-01",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 31)
            };
            var debit = new LedgerAccount
            {
                Code = "110100",
                NameAr = "النقدية",
                Type = LedgerAccountType.Asset,
                NormalBalance = AccountNormalBalance.Debit
            };
            var credit = new LedgerAccount
            {
                Code = "410100",
                NameAr = "الإيرادات",
                Type = LedgerAccountType.Revenue,
                NormalBalance = AccountNormalBalance.Credit
            };
            Context.AddRange(year, period, debit, credit);
            Context.SaveChanges();
            FiscalYearId = year.Id;
            PeriodId = period.Id;
            DebitAccountId = debit.Id;
            CreditAccountId = credit.Id;
        }

        public int SeedCompletedSalesOrder(decimal total, decimal vat)
        {
            var customer = new Customer
            {
                Name = "Accounting Test Customer",
                Type = CustomerType.Wholesale,
                Status = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var order = new SalesOrder
            {
                OrderNumber = $"SO-{Guid.NewGuid():N}",
                OrderDate = new DateTime(2026, 1, 15),
                Customer = customer,
                Status = SalesOrderStatus.Completed,
                SubTotal = total - vat,
                VATAmount = vat,
                TotalAmount = total,
                RemainingAmount = total,
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(order);
            Context.SaveChanges();
            return order.Id;
        }

        public int SeedApprovedPurchaseReceiving(decimal total, decimal vat)
        {
            var supplier = new Supplier
            {
                Name = "Accounting Test Supplier",
                Type = SupplierType.Feed,
                Status = SupplierStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var order = new PurchaseOrder
            {
                OrderNumber = $"PO-{Guid.NewGuid():N}",
                OrderDate = new DateTime(2026, 1, 15),
                ExpectedDeliveryDate = new DateTime(2026, 1, 20),
                ActualDeliveryDate = new DateTime(2026, 1, 20),
                Supplier = supplier,
                Status = PurchaseOrderStatus.Received,
                IsReceived = true,
                SubTotal = total - vat,
                AmountAfterDiscount = total - vat,
                VATAmount = vat,
                Total = total,
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(order);
            Context.SaveChanges();
            var receiving = new PurchaseReceiving
            {
                ReceivingNumber = $"GRN-{Guid.NewGuid():N}",
                PurchaseOrderId = order.Id,
                ReceivingDate = new DateTime(2026, 1, 20),
                IsFullReceiving = true,
                QualityInspectionCompleted = true,
                OverallQualityResult = QualityTestResult.Passed,
                SupplierInvoiceNumber = $"INV-{Guid.NewGuid():N}",
                SupplierInvoiceAmount = total,
                ApprovedBy = "purchasing-approver",
                ApprovedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(receiving);
            Context.SaveChanges();
            return receiving.Id;
        }

        public void ApprovePilotConfiguration()
        {
            var service = new AccountingConfigurationService(Context);
            var configuration = service.CreatePilotDraft("configuration-owner");
            service.Approve(configuration.Id, "independent-accountant", "Pilot map reviewed");
        }

        public int AccountId(string code) => Context.LedgerAccounts.Single(item => item.Code == code).Id;

        public (int DebitId, int CreditId) SeedForeignCurrencyAccounts()
        {
            var debit = new LedgerAccount
            {
                Code = $"USD-A-{Guid.NewGuid():N}",
                NameAr = "حساب أصل بالدولار",
                Type = LedgerAccountType.Asset,
                NormalBalance = AccountNormalBalance.Debit,
                CurrencyCode = "USD"
            };
            var credit = new LedgerAccount
            {
                Code = $"USD-E-{Guid.NewGuid():N}",
                NameAr = "حساب حقوق ملكية بالدولار",
                Type = LedgerAccountType.Equity,
                NormalBalance = AccountNormalBalance.Credit,
                CurrencyCode = "USD"
            };
            Context.AddRange(debit, credit);
            Context.SaveChanges();
            return (debit.Id, credit.Id);
        }

        public void ChangeUsedDebitAccountCurrency(string currency) =>
            Context.LedgerAccounts.Single(item => item.Id == DebitAccountId).CurrencyCode = currency;

        public int AddNextYearJanuaryPeriod()
        {
            var year = new FiscalYear
            {
                Name = "FY2027",
                StartDate = new DateTime(2027, 1, 1),
                EndDate = new DateTime(2027, 12, 31)
            };
            var period = new FiscalPeriod
            {
                FiscalYear = year,
                Name = "2027-01",
                StartDate = new DateTime(2027, 1, 1),
                EndDate = new DateTime(2027, 1, 31)
            };
            Context.Add(period);
            Context.SaveChanges();
            return period.Id;
        }

        public void PostRevenue(decimal amount)
        {
            var ledger = new GeneralLedgerService(Context);
            var draft = ledger.CreateDraft(BalancedRequest(amount), "revenue-accountant", "Recognized annual revenue");
            ledger.Approve(draft.Id, "revenue-approver", "Revenue evidence checked");
            ledger.Post(draft.Id, "posting-controller", "Revenue posted");
        }

        public void PrepareFinalPeriod()
        {
            new GeneralLedgerService(Context).ClosePeriod(PeriodId, "period-controller", "January reconciled");
            for (var month = 2; month <= 12; month++)
            {
                var start = new DateTime(2026, month, 1);
                Context.FiscalPeriods.Add(new FiscalPeriod
                {
                    FiscalYearId = FiscalYearId,
                    Name = $"2026-{month:D2}",
                    StartDate = start,
                    EndDate = start.AddMonths(1).AddDays(-1)
                });
            }
            Context.SaveChanges();
            var ledger = new GeneralLedgerService(Context);
            foreach (var periodId in Context.FiscalPeriods
                         .Where(item => item.FiscalYearId == FiscalYearId && item.EndDate < new DateTime(2026, 12, 1))
                         .Select(item => item.Id).ToArray())
            {
                ledger.ClosePeriod(periodId, "period-controller", "Monthly reconciliation completed");
            }
        }

        public JournalEntry PostJournal(
            string description,
            string source,
            IReadOnlyList<(string Code, decimal Debit, decimal Credit)> lines)
        {
            var ledger = new GeneralLedgerService(Context);
            var draft = ledger.CreateDraft(new JournalDraftRequest(
                new DateTime(2026, 1, 25),
                PeriodId,
                description,
                source,
                Guid.NewGuid().ToString("N"),
                lines.Select(item => new JournalLineRequest(AccountId(item.Code), item.Debit, item.Credit)).ToArray()),
                "statement-accountant",
                "Statement test journal");
            ledger.Approve(draft.Id, "statement-approver", "Statement journal checked");
            return ledger.Post(draft.Id, "posting-controller", "Statement journal posted");
        }

        public JournalEntry CreateDraftJournal(
            string description,
            IReadOnlyList<(string Code, decimal Debit, decimal Credit)> lines) =>
            new GeneralLedgerService(Context).CreateDraft(new JournalDraftRequest(
                new DateTime(2026, 1, 26),
                PeriodId,
                description,
                "DraftOnly",
                Guid.NewGuid().ToString("N"),
                lines.Select(item => new JournalLineRequest(AccountId(item.Code), item.Debit, item.Credit)).ToArray()),
                "draft-accountant",
                "Must remain outside reports");

        public (string Code, decimal Debit, decimal Credit)[] LinesWithAccountCodes(long entryId) =>
            Context.JournalEntryLines.AsNoTracking()
                .Where(line => line.JournalEntryId == entryId)
                .Select(line => new { line.LedgerAccount.Code, line.Debit, line.Credit })
                .AsEnumerable().Select(line => (line.Code, line.Debit, line.Credit)).ToArray();

        public int SeedCompletedCustomerPayment(decimal amount)
        {
            var customer = new Customer
            {
                Name = "Payment Customer",
                Type = CustomerType.Wholesale,
                Status = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var payment = new CustomerPayment
            {
                Customer = customer,
                PaymentNumber = $"CP-{Guid.NewGuid():N}",
                PaymentDate = new DateTime(2026, 1, 21),
                Amount = amount,
                PaymentMethod = "BankTransfer",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(payment);
            Context.SaveChanges();
            return payment.Id;
        }

        public int SeedCompletedSupplierPayment(decimal amount)
        {
            var supplier = new Supplier
            {
                Name = "Payment Supplier",
                Type = SupplierType.Feed,
                Status = SupplierStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var payment = new SupplierPayment
            {
                Supplier = supplier,
                PaymentNumber = $"SP-{Guid.NewGuid():N}",
                PaymentDate = new DateTime(2026, 1, 22),
                Amount = amount,
                PaymentMethod = "BankTransfer",
                Status = "Completed",
                PaidBy = "treasurer",
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(payment);
            Context.SaveChanges();
            return payment.Id;
        }

        public int SeedApprovedSalary(decimal gross, decimal net, decimal deductions)
        {
            var employee = new Employee
            {
                EmployeeNumber = $"EMP-{Guid.NewGuid():N}",
                FullName = "Payroll Employee",
                Name = "Payroll Employee",
                NationalId = "1234567890",
                BirthDate = new DateTime(1990, 1, 1),
                HireDate = new DateTime(2020, 1, 1),
                Position = EmployeePosition.Accountant,
                Department = EmployeeDepartment.Accounting,
                EmploymentType = EmploymentType.FullTime,
                Status = EmployeeStatus.Active,
                BasicSalary = gross,
                CreatedAt = DateTime.UtcNow
            };
            var salary = new Salary
            {
                Employee = employee,
                SalaryNumber = $"SAL-{Guid.NewGuid():N}",
                Month = 1,
                Year = 2026,
                PayPeriodStart = new DateTime(2026, 1, 1),
                PayPeriodEnd = new DateTime(2026, 1, 31),
                BasicSalary = gross,
                GrossSalary = gross,
                TotalDeductions = deductions,
                NetSalary = net,
                Status = SalaryStatus.Approved,
                ApprovedBy = "payroll-approver",
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(salary);
            Context.SaveChanges();
            return salary.Id;
        }

        public int SeedApprovedDepreciation(decimal amount)
        {
            var asset = new FixedAsset
            {
                AssetNumber = $"AST-{Guid.NewGuid():N}",
                AssetName = "Test Pump",
                Category = AssetCategory.Pumps,
                PurchaseDate = new DateTime(2025, 1, 1),
                PurchaseCost = 12_000m,
                ResidualValue = 0m,
                UsefulLifeYears = 8,
                DepreciationMethod = DepreciationMethod.StraightLine,
                Status = AssetStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            var depreciation = new AssetDepreciation
            {
                FixedAsset = asset,
                Year = 2026,
                Month = 1,
                DepreciationDate = new DateTime(2026, 1, 31),
                OpeningBookValue = 10_000m,
                DepreciationAmount = amount,
                AccumulatedDepreciation = 2_000m + amount,
                ClosingBookValue = 10_000m - amount,
                IsApproved = true,
                ApprovedBy = "asset-approver",
                ApprovedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            Context.Add(depreciation);
            Context.SaveChanges();
            return depreciation.Id;
        }

        public void Dispose()
        {
            Context.Dispose();
            if (File.Exists(_databasePath)) File.Delete(_databasePath);
        }
    }
}
