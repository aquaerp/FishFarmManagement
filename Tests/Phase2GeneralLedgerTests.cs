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
        Assert.Equal(5L, Convert.ToInt64(command.ExecuteScalar()));
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

    private sealed class LedgerTestDatabase : IDisposable
    {
        private readonly string _databasePath;
        public FishFarmContext Context { get; }
        public int PeriodId { get; private set; }
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

        public void Dispose()
        {
            Context.Dispose();
            if (File.Exists(_databasePath)) File.Delete(_databasePath);
        }
    }
}
