using FishFarmManager.Data;
using FishFarmManager.Forms;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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
        Assert.Equal(9L, Convert.ToInt64(command.ExecuteScalar()));
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
    public void ApprovedInventoryMovements_PostDetailedCostFlowsAndRejectAmbiguousTypes()
    {
        using var database = LedgerTestDatabase.Create();
        database.ApprovePilotConfiguration();
        var saleId = database.SeedApprovedStockMovement(StockMovementType.Sale, 100m);
        var wasteId = database.SeedApprovedStockMovement(StockMovementType.Waste, 40m);
        var increaseId = database.SeedApprovedStockMovement(StockMovementType.AdjustmentIncrease, 25m);
        var purchaseId = database.SeedApprovedStockMovement(StockMovementType.Purchase, 60m);
        var posting = new OperationalPostingService(database.Context);

        var sale = posting.CreateInventoryMovementDraft(saleId, database.PeriodId, "inventory-accountant", "Approved sale cost");
        var waste = posting.CreateInventoryMovementDraft(wasteId, database.PeriodId, "inventory-accountant", "Approved waste write-off");
        var increase = posting.CreateInventoryMovementDraft(increaseId, database.PeriodId, "inventory-accountant", "Approved count increase");

        Assert.Contains(database.LinesWithAccountCodes(sale.Id), line => line.Code == "5100" && line.Debit == 100m);
        Assert.Contains(database.LinesWithAccountCodes(sale.Id), line => line.Code == "1130" && line.Credit == 100m);
        Assert.Contains(database.LinesWithAccountCodes(waste.Id), line => line.Code == "5410" && line.Debit == 40m);
        Assert.Contains(database.LinesWithAccountCodes(increase.Id), line => line.Code == "1130" && line.Debit == 25m);
        Assert.Contains(database.LinesWithAccountCodes(increase.Id), line => line.Code == "4200" && line.Credit == 25m);
        Assert.Throws<InvalidOperationException>(() => posting.CreateInventoryMovementDraft(
            saleId, database.PeriodId, "inventory-accountant", "Duplicate sale cost"));
        Assert.Throws<InvalidOperationException>(() => posting.CreateInventoryMovementDraft(
            purchaseId, database.PeriodId, "inventory-accountant", "Must use purchase receipt"));
        Assert.Equal(3, database.Context.OperationalPostingRecords.Count());
    }

    [Fact]
    public void SubmittedVatReturns_ReclassifyToPayableOrReceivableAndRequireReconciliation()
    {
        using var database = LedgerTestDatabase.Create();
        database.ApprovePilotConfiguration();
        var payableId = database.SeedSubmittedVatReturn(150m, 30m, "202601");
        var receivableId = database.SeedSubmittedVatReturn(20m, 50m, "202602");
        var adjustedId = database.SeedSubmittedVatReturn(75m, 25m, "202603");
        var adjusted = database.Context.VATReturns.Single(value => value.Id == adjustedId);
        adjusted.Box12_Adjustments = 5m;
        adjusted.Box15_NetVATDueForPeriod = 55m;
        database.Context.SaveChanges();
        var posting = new OperationalPostingService(database.Context);

        var payable = posting.CreateVatReturnSettlementDraft(
            payableId, database.PeriodId, "tax-accountant", "Submitted VAT payable return");
        var receivable = posting.CreateVatReturnSettlementDraft(
            receivableId, database.PeriodId, "tax-accountant", "Submitted VAT refund return");
        var payableLines = database.LinesWithAccountCodes(payable.Id);
        var receivableLines = database.LinesWithAccountCodes(receivable.Id);

        Assert.Contains(payableLines, line => line.Code == "2200" && line.Debit == 150m);
        Assert.Contains(payableLines, line => line.Code == "1140" && line.Credit == 30m);
        Assert.Contains(payableLines, line => line.Code == "2210" && line.Credit == 120m);
        Assert.Contains(receivableLines, line => line.Code == "2200" && line.Debit == 20m);
        Assert.Contains(receivableLines, line => line.Code == "1140" && line.Credit == 50m);
        Assert.Contains(receivableLines, line => line.Code == "1150" && line.Debit == 30m);
        Assert.Throws<InvalidOperationException>(() => posting.CreateVatReturnSettlementDraft(
            adjustedId, database.PeriodId, "tax-accountant", "Unsupported VAT adjustment"));
    }

    [Fact]
    public void WeightedAverageInventory_ApprovesAtomicallyAndPreventsNegativeOrMutableHistory()
    {
        using var database = LedgerTestDatabase.Create();
        var item = new InventoryItem
        {
            Name = "Weighted average feed", Category = InventoryCategory.Feed, Unit = "kg",
            CurrentStock = 10m, MinimumStock = 0m, MaximumStock = 1_000m,
            UnitCost = 20m, Status = InventoryStatus.Active, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        database.Context.Add(item);
        database.Context.SaveChanges();
        var service = new InventoryTransactionService(database.Context);
        var receipt = service.CreateDraft(new InventoryMovementDraftRequest(
            item.Id, StockMovementType.Purchase, 10m, 10m, new DateTime(2026, 1, 10), "GRN-WAC-001"),
            "warehouse-maker", "Received approved feed lot");

        Assert.Throws<InvalidOperationException>(() => service.Approve(
            receipt.Id, "warehouse-maker", "Self approval must fail"));
        var receiptResult = service.Approve(receipt.Id, "warehouse-reviewer", "Receipt and invoice matched");
        Assert.Equal(20m, receiptResult.QuantityAfter);
        Assert.Equal(15m, receiptResult.WeightedAverageUnitCost);
        Assert.Equal(100m, receiptResult.MovementValue);

        var issue = service.CreateDraft(new InventoryMovementDraftRequest(
            item.Id, StockMovementType.Sale, 5m, null, new DateTime(2026, 1, 15), "SO-WAC-001"),
            "warehouse-maker", "Issue for completed sale");
        var issueResult = service.Approve(issue.Id, "warehouse-reviewer", "Sale issue checked");
        Assert.Equal(15m, issueResult.QuantityAfter);
        Assert.Equal(75m, issueResult.MovementValue);
        Assert.Equal(2, database.Context.InventoryValuations.Count(value => value.Method == ValuationMethod.WeightedAverage));

        var excessiveIssue = service.CreateDraft(new InventoryMovementDraftRequest(
            item.Id, StockMovementType.Consumption, 16m, null, new DateTime(2026, 1, 16), "ISS-WAC-002"),
            "warehouse-maker", "Excessive issue test");
        Assert.Throws<InvalidOperationException>(() => service.Approve(
            excessiveIssue.Id, "warehouse-reviewer", "Must not create negative inventory"));
        Assert.Equal(15m, database.Context.InventoryItems.AsNoTracking().Single(value => value.Id == item.Id).CurrentStock);

        database.Context.ChangeTracker.Clear();
        var storedItem = database.Context.InventoryItems.Single(value => value.Id == item.Id);
        storedItem.CurrentStock = -1m;
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
        database.Context.ChangeTracker.Clear();
        var storedMovement = database.Context.StockMovements.Single(value => value.Id == issue.Id);
        storedMovement.Notes = "Attempted history rewrite";
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void PurchaseReceivingApproval_UpdatesOrderInventoryAndMovementsAllOrNothing()
    {
        using var database = LedgerTestDatabase.Create();
        var receivingId = database.SeedPurchaseReceivingForInventory(secondUnitPrice: 0m);
        var service = new PurchaseReceivingInventoryService(database.Context);

        Assert.Throws<InvalidOperationException>(() => service.ApproveAndAddToInventory(
            receivingId, "receiving-controller", "All received items checked"));
        Assert.Empty(database.Context.StockMovements);
        Assert.All(database.Context.InventoryItems.AsNoTracking()
            .Where(value => value.Name.StartsWith("Atomic receiving")), value => Assert.Equal(0m, value.CurrentStock));
        Assert.Null(database.Context.PurchaseReceivings.AsNoTracking()
            .Single(value => value.Id == receivingId).ApprovedDate);

        var invalidItem = database.Context.PurchaseReceivingItems
            .Single(value => value.PurchaseReceivingId == receivingId && value.UnitPrice == 0m);
        invalidItem.UnitPrice = 20m;
        database.Context.SaveChanges();
        var result = service.ApproveAndAddToInventory(
            receivingId, "receiving-controller", "All received items checked");

        Assert.Equal(2, result.InventoryMovements.Count);
        Assert.Equal("receiving-controller", result.Receiving.ApprovedBy);
        Assert.All(result.Receiving.Items, value =>
        {
            Assert.True(value.AddedToStock);
            Assert.NotNull(value.StockMovementId);
        });
        var storedItems = database.Context.InventoryItems.AsNoTracking()
            .Where(value => value.Name.StartsWith("Atomic receiving")).OrderBy(value => value.Name).ToArray();
        Assert.Equal(5m, storedItems[0].CurrentStock);
        Assert.Equal(10m, storedItems[0].UnitCost);
        Assert.Equal(3m, storedItems[1].CurrentStock);
        Assert.Equal(20m, storedItems[1].UnitCost);
        var order = database.Context.PurchaseOrders.AsNoTracking().Single(value => value.Id == result.Receiving.PurchaseOrderId);
        Assert.True(order.IsReceived);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Throws<InvalidOperationException>(() => service.ApproveAndAddToInventory(
            receivingId, "receiving-controller-2", "Duplicate receipt"));
        Assert.Equal(2, database.Context.StockMovements.Count());
    }

    [Fact]
    public void InventoryDraftMaintenance_IsOwnerOnlyAuditedAndDoesNotChangeStock()
    {
        using var database = LedgerTestDatabase.Create();
        var item = new InventoryItem
        {
            Name = "Draft workflow item", Category = InventoryCategory.Feed, Unit = "kg",
            CurrentStock = 10m, MinimumStock = 0m, MaximumStock = 100m,
            UnitCost = 10m, Status = InventoryStatus.Active, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        database.Context.Add(item);
        database.Context.SaveChanges();
        var service = new InventoryTransactionService(database.Context);
        var draft = service.CreateDraft(new InventoryMovementDraftRequest(
            item.Id, StockMovementType.Sale, 2m, null, new DateTime(2026, 1, 15), "DRAFT-001"),
            "movement-maker", "Initial draft");

        var updated = service.UpdateDraft(draft.Id, new InventoryMovementDraftRequest(
            item.Id, StockMovementType.Consumption, 3m, null, new DateTime(2026, 1, 16), "DRAFT-001-R1"),
            "movement-maker", "Correct movement classification");
        Assert.Equal(3m, updated.Quantity);
        Assert.Equal(StockMovementType.Consumption, updated.MovementType);
        Assert.Equal(10m, database.Context.InventoryItems.AsNoTracking().Single(value => value.Id == item.Id).CurrentStock);
        Assert.Throws<InvalidOperationException>(() => service.UpdateDraft(draft.Id,
            new InventoryMovementDraftRequest(item.Id, StockMovementType.Sale, 1m, null,
                new DateTime(2026, 1, 17), "DRAFT-OTHER", null),
            "other-user", "Unauthorized draft change"));

        service.DeleteDraft(draft.Id, "movement-maker", "Draft no longer required");
        Assert.Empty(database.Context.StockMovements);
        Assert.Equal(10m, database.Context.InventoryItems.AsNoTracking().Single(value => value.Id == item.Id).CurrentStock);
        var actions = database.Context.AccountingAuditEvents.AsNoTracking()
            .Where(value => value.EntityType == nameof(StockMovement) && value.EntityId == draft.Id.ToString())
            .Select(value => value.Action).ToArray();
        Assert.Contains("CreateDraft", actions);
        Assert.Contains("UpdateDraft", actions);
        Assert.Contains("DeleteDraft", actions);
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

    [Fact]
    public void AccountingAdjustment_RequiresSupportingEvidenceAndFutureReversalDate()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new AccountingAdjustmentService(database.Context);
        var baseRequest = database.BalancedRequest(45m);

        Assert.Throws<InvalidOperationException>(() => service.CreateDraft(new AccountingAdjustmentRequest(
            baseRequest.EntryDate,
            baseRequest.FiscalPeriodId,
            "Accrued utility expense",
            AccountingAdjustmentType.Accrual,
            " ",
            new DateTime(2026, 1, 20),
            baseRequest.Lines), "adjustment-accountant", "Month-end accrual"));
        Assert.Throws<InvalidOperationException>(() => service.CreateDraft(new AccountingAdjustmentRequest(
            baseRequest.EntryDate,
            baseRequest.FiscalPeriodId,
            "Accrued utility expense",
            AccountingAdjustmentType.Accrual,
            "SUPPORT-001",
            baseRequest.EntryDate,
            baseRequest.Lines), "adjustment-accountant", "Invalid reversal date"));

        var result = service.CreateDraft(new AccountingAdjustmentRequest(
            baseRequest.EntryDate,
            baseRequest.FiscalPeriodId,
            "Accrued utility expense",
            AccountingAdjustmentType.Accrual,
            "SUPPORT-001",
            new DateTime(2026, 1, 20),
            baseRequest.Lines), "adjustment-accountant", "Month-end accrual");

        Assert.Equal(JournalEntryStatus.Draft, result.JournalEntry.Status);
        Assert.Equal("AccountingAdjustment", result.JournalEntry.Source);
        Assert.Equal(result.JournalEntry.Id, result.Adjustment.JournalEntryId);
        Assert.Equal("SUPPORT-001", result.Adjustment.SupportingDocumentReference);
    }

    [Fact]
    public void PostedAdjustment_AutoReversesOnceWhenDueAndRecordIsImmutable()
    {
        using var database = LedgerTestDatabase.Create();
        var baseRequest = database.BalancedRequest(65m);
        var service = new AccountingAdjustmentService(database.Context);
        var result = service.CreateDraft(new AccountingAdjustmentRequest(
            baseRequest.EntryDate,
            baseRequest.FiscalPeriodId,
            "Accrued professional fees",
            AccountingAdjustmentType.Accrual,
            "ENGAGEMENT-2026-01",
            new DateTime(2026, 1, 20),
            baseRequest.Lines), "adjustment-accountant", "Supported month-end accrual");
        var ledger = new GeneralLedgerService(database.Context);
        ledger.Approve(result.JournalEntry.Id, "adjustment-approver", "Supporting evidence checked");
        ledger.Post(result.JournalEntry.Id, "posting-controller", "Adjustment posted");

        Assert.Throws<InvalidOperationException>(() => service.ReverseDue(
            result.Adjustment.Id, new DateTime(2026, 1, 19), "reversal-controller", "Not due"));
        var reversal = service.ReverseDue(
            result.Adjustment.Id, new DateTime(2026, 1, 20), "reversal-controller", "Scheduled reversal due");
        var stored = database.Context.AccountingAdjustments.Single(item => item.Id == result.Adjustment.Id);
        var lines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(item => item.JournalEntryId == reversal.Id).OrderBy(item => item.LineNumber).ToArray();

        Assert.Equal(JournalEntryStatus.Posted, reversal.Status);
        Assert.Equal(reversal.Id, stored.ReversalJournalEntryId);
        Assert.Equal(65m, lines[0].Credit);
        Assert.Equal(65m, lines[1].Debit);
        Assert.Throws<InvalidOperationException>(() => service.ReverseDue(
            result.Adjustment.Id, new DateTime(2026, 1, 21), "reversal-controller", "Duplicate reversal"));

        stored.SupportingDocumentReference = "TAMPERED";
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void ForeignExchangeRate_ValidatesCurrencyPrecisionAndEvidence()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new ForeignExchangeRateService(database.Context);

        Assert.Throws<InvalidOperationException>(() => service.CreateDraft(new(
            "SAR", new DateTime(2026, 7, 15), ExchangeRatePurpose.Transaction, 1m,
            "SAMA", "evidence/sar.pdf"), "rate-maker", "Invalid functional currency"));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateDraft(new(
            "USD", new DateTime(2026, 7, 15), ExchangeRatePurpose.Transaction, 3.750000001m,
            "SAMA", "evidence/usd.pdf"), "rate-maker", "Excess precision"));
        Assert.Throws<ArgumentException>(() => service.CreateDraft(new(
            "USD", new DateTime(2026, 7, 15), ExchangeRatePurpose.Transaction, 3.75m,
            "SAMA", ""), "rate-maker", "Missing evidence"));
    }

    [Fact]
    public void ForeignExchangeRate_RequiresIndependentApprovalAndExactDate()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new ForeignExchangeRateService(database.Context);
        var draft = service.CreateDraft(new(
            "usd", new DateTime(2026, 7, 15, 14, 30, 0), ExchangeRatePurpose.Transaction, 3.75m,
            "https://www.sama.gov.sa/rates", "evidence/USD-20260715.pdf"),
            "rate-maker", "Daily transaction rate");

        Assert.Equal("USD", draft.CurrencyCode);
        Assert.Equal(new DateTime(2026, 7, 15), draft.RateDate);
        Assert.Throws<InvalidOperationException>(() =>
            service.Approve(draft.Id, "rate-maker", "Self approval"));

        service.Approve(draft.Id, "rate-reviewer", "Source and evidence verified");
        var approved = service.GetApprovedRate("USD", new DateTime(2026, 7, 15), ExchangeRatePurpose.Transaction);

        Assert.Equal(ExchangeRateStatus.Approved, approved.Status);
        Assert.Equal("rate-reviewer", approved.ApprovedBy);
        Assert.Equal(2, database.Context.AccountingAuditEvents.Count(item =>
            item.EntityType == nameof(ForeignExchangeRate) && item.EntityId == draft.Id.ToString()));
        Assert.Throws<InvalidOperationException>(() => service.GetApprovedRate(
            "USD", new DateTime(2026, 7, 14), ExchangeRatePurpose.Transaction));
        Assert.Throws<InvalidOperationException>(() => service.GetApprovedRate(
            "USD", new DateTime(2026, 7, 15), ExchangeRatePurpose.Closing));
    }

    [Fact]
    public void ForeignExchangeRate_NewApprovalRetiresPriorVersion()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new ForeignExchangeRateService(database.Context);
        var first = service.CreateDraft(new(
            "EUR", new DateTime(2026, 7, 15), ExchangeRatePurpose.Closing, 4.31m,
            "SAMA rate publication", "evidence/EUR-v1.pdf"), "maker-one", "Initial closing rate");
        service.Approve(first.Id, "reviewer-one", "Initial evidence verified");
        var replacement = service.CreateDraft(new(
            "EUR", new DateTime(2026, 7, 15), ExchangeRatePurpose.Closing, 4.32m,
            "Corrected SAMA publication", "evidence/EUR-v2.pdf"), "maker-two", "Correct source error");

        service.Approve(replacement.Id, "reviewer-two", "Correction evidence verified");

        Assert.Equal(2, replacement.Version);
        Assert.Equal(ExchangeRateStatus.Retired,
            database.Context.ForeignExchangeRates.AsNoTracking().Single(item => item.Id == first.Id).Status);
        Assert.Equal(replacement.Id, service.GetApprovedRate(
            "EUR", new DateTime(2026, 7, 15), ExchangeRatePurpose.Closing).Id);
    }

    [Fact]
    public void ForeignExchangeRate_ApprovedValueIsImmutableAtDatabaseLevel()
    {
        using var database = LedgerTestDatabase.Create();
        var service = new ForeignExchangeRateService(database.Context);
        var rate = service.CreateDraft(new(
            "GBP", new DateTime(2026, 7, 15), ExchangeRatePurpose.Transaction, 4.85m,
            "SAMA rate publication", "evidence/GBP.pdf"), "rate-maker", "Transaction rate");
        service.Approve(rate.Id, "rate-reviewer", "Evidence verified");

        rate.SarPerUnit = 4.90m;

        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void GeneralLedger_PersistsApprovedForeignMeasurementAndReversesIt()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var rate = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 3.75m,
            "SAMA rate publication", "evidence/USD-20260115.pdf"), "rate-maker", "Transaction rate");
        rateService.Approve(rate.Id, "rate-reviewer", "Rate evidence verified");
        var baseRequest = database.BalancedRequest(375m);
        var request = baseRequest with
        {
            Lines = new[]
            {
                baseRequest.Lines[0] with
                {
                    ForeignCurrencyCode = "usd", ForeignAmount = 100m, ForeignExchangeRateId = rate.Id
                },
                baseRequest.Lines[1]
            }
        };
        var ledger = new GeneralLedgerService(database.Context);

        var draft = ledger.CreateDraft(request, "journal-maker", "Foreign sale recognition");
        ledger.Approve(draft.Id, "journal-reviewer", "Translation checked");
        ledger.Post(draft.Id, "journal-poster", "Foreign journal posted");
        var replacement = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 3.76m,
            "Corrected SAMA publication", "evidence/USD-20260115-v2.pdf"), "rate-corrector", "Correct rate");
        rateService.Approve(replacement.Id, "rate-controller", "Correction verified");
        var reversal = ledger.Reverse(draft.Id, new DateTime(2026, 1, 20), "journal-controller", "Cancel transaction");
        var originalLine = database.Context.JournalEntryLines.AsNoTracking()
            .Single(line => line.JournalEntryId == draft.Id && line.ForeignExchangeRateId != null);
        var reversalLine = database.Context.JournalEntryLines.AsNoTracking()
            .Single(line => line.JournalEntryId == reversal.Id && line.ForeignExchangeRateId != null);

        Assert.Equal("USD", originalLine.ForeignCurrencyCode);
        Assert.Equal(100m, originalLine.ForeignAmount);
        Assert.Equal(3.75m, originalLine.ExchangeRateSarPerUnit);
        Assert.Equal(375m, reversalLine.Credit);
        Assert.Equal(originalLine.ForeignAmount, reversalLine.ForeignAmount);
        Assert.Equal(originalLine.ForeignExchangeRateId, reversalLine.ForeignExchangeRateId);
    }

    [Fact]
    public void GeneralLedger_RejectsIncompleteOrMismatchedForeignMeasurement()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var rate = rateService.CreateDraft(new(
            "EUR", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 4m,
            "SAMA rate publication", "evidence/EUR-20260115.pdf"), "rate-maker", "Transaction rate");
        rateService.Approve(rate.Id, "rate-reviewer", "Rate evidence verified");
        var ledger = new GeneralLedgerService(database.Context);
        var baseRequest = database.BalancedRequest(400m);

        Assert.Throws<InvalidOperationException>(() => ledger.CreateDraft(baseRequest with
        {
            Lines = new[]
            {
                baseRequest.Lines[0] with { ForeignCurrencyCode = "EUR", ForeignAmount = 100m },
                baseRequest.Lines[1]
            }
        }, "journal-maker", "Missing rate"));
        Assert.Throws<InvalidOperationException>(() => ledger.CreateDraft(baseRequest with
        {
            Lines = new[]
            {
                baseRequest.Lines[0] with
                {
                    Debit = 399.99m, ForeignCurrencyCode = "EUR", ForeignAmount = 100m,
                    ForeignExchangeRateId = rate.Id
                },
                baseRequest.Lines[1] with { Credit = 399.99m }
            }
        }, "journal-maker", "Incorrect translation"));
    }

    [Fact]
    public void GeneralLedger_ForeignMeasurementIsProtectedAtDatabaseLevel()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var rate = rateService.CreateDraft(new(
            "GBP", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 5m,
            "SAMA rate publication", "evidence/GBP-20260115.pdf"), "rate-maker", "Transaction rate");
        rateService.Approve(rate.Id, "rate-reviewer", "Rate evidence verified");
        var baseRequest = database.BalancedRequest(500m);
        var request = baseRequest with
        {
            Lines = new[]
            {
                baseRequest.Lines[0] with
                {
                    ForeignCurrencyCode = "GBP", ForeignAmount = 100m, ForeignExchangeRateId = rate.Id
                },
                baseRequest.Lines[1]
            }
        };
        var draft = new GeneralLedgerService(database.Context)
            .CreateDraft(request, "journal-maker", "Protected foreign measurement");
        var line = database.Context.JournalEntryLines.Single(item =>
            item.JournalEntryId == draft.Id && item.ForeignExchangeRateId != null);

        line.ForeignAmount = 99m;

        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void ForeignMonetaryItem_FullAssetSettlementPostsRealizedGain()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var recognitionRate = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 3.75m,
            "SAMA recognition rate", "evidence/USD-recognition.pdf"), "rate-maker", "Recognition rate");
        rateService.Approve(recognitionRate.Id, "rate-reviewer", "Recognition rate verified");
        var baseRequest = database.BalancedRequest(375m);
        var recognitionRequest = baseRequest with
        {
            Lines = new[]
            {
                baseRequest.Lines[0] with
                {
                    ForeignCurrencyCode = "USD", ForeignAmount = 100m,
                    ForeignExchangeRateId = recognitionRate.Id
                },
                baseRequest.Lines[1]
            }
        };
        var ledger = new GeneralLedgerService(database.Context);
        var recognition = ledger.CreateDraft(recognitionRequest, "sales-accountant", "Foreign receivable");
        ledger.Approve(recognition.Id, "sales-reviewer", "Receivable checked");
        ledger.Post(recognition.Id, "posting-controller", "Receivable posted");
        var recognitionLine = database.Context.JournalEntryLines.Single(line =>
            line.JournalEntryId == recognition.Id && line.ForeignExchangeRateId != null);
        var service = new ForeignCurrencyMonetaryItemService(database.Context);
        var item = service.Register(new(recognitionLine.Id, ForeignMonetaryItemKind.Asset, "AR-USD-001"),
            "subledger-accountant", "Register posted receivable");
        Assert.Throws<InvalidOperationException>(() => ledger.Reverse(
            recognition.Id, new DateTime(2026, 1, 18), "controller", "Generic reversal forbidden"));
        var settlementRate = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 20), ExchangeRatePurpose.Transaction, 3.80m,
            "SAMA settlement rate", "evidence/USD-settlement.pdf"), "rate-maker", "Settlement rate");
        rateService.Approve(settlementRate.Id, "rate-reviewer", "Settlement rate verified");
        var accounts = database.SeedFxSettlementAccounts();

        var result = service.Settle(new(
                item.Id, new DateTime(2026, 1, 20), database.PeriodId, 100m, settlementRate.Id,
                accounts.CashId, accounts.GainId, accounts.LossId, "RCPT-USD-001"),
            "settlement-maker", "settlement-reviewer", "settlement-poster", "USD receipt settled");
        var lines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntryId == result.JournalEntry.Id).ToArray();

        Assert.Equal(JournalEntryStatus.Posted, result.JournalEntry.Status);
        Assert.Equal(ForeignMonetaryItemStatus.Settled, result.Item.Status);
        Assert.Equal(0m, result.Item.OutstandingForeignAmount);
        Assert.Equal(0m, result.Item.CarryingAmountSar);
        Assert.Equal(5m, result.Settlement.RealizedGainLossSar);
        Assert.Contains(lines, line => line.LedgerAccountId == accounts.CashId
            && line.Debit == 380m && line.ForeignAmount == 100m);
        Assert.Contains(lines, line => line.LedgerAccountId == item.LedgerAccountId && line.Credit == 375m);
        Assert.Contains(lines, line => line.LedgerAccountId == accounts.GainId && line.Credit == 5m);
    }

    [Fact]
    public void ForeignMonetaryItem_PartialSettlementPreservesBalanceAndSettlementIsImmutable()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var recognitionRate = rateService.CreateDraft(new(
            "EUR", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 4m,
            "SAMA recognition rate", "evidence/EUR-recognition.pdf"), "rate-maker", "Recognition rate");
        rateService.Approve(recognitionRate.Id, "rate-reviewer", "Recognition rate verified");
        var baseRequest = database.BalancedRequest(400m);
        var recognitionRequest = baseRequest with
        {
            Lines = new[]
            {
                baseRequest.Lines[0] with
                {
                    ForeignCurrencyCode = "EUR", ForeignAmount = 100m,
                    ForeignExchangeRateId = recognitionRate.Id
                },
                baseRequest.Lines[1]
            }
        };
        var ledger = new GeneralLedgerService(database.Context);
        var recognition = ledger.CreateDraft(recognitionRequest, "sales-accountant", "EUR receivable");
        ledger.Approve(recognition.Id, "sales-reviewer", "Receivable checked");
        ledger.Post(recognition.Id, "posting-controller", "Receivable posted");
        var recognitionLine = database.Context.JournalEntryLines.Single(line =>
            line.JournalEntryId == recognition.Id && line.ForeignExchangeRateId != null);
        var service = new ForeignCurrencyMonetaryItemService(database.Context);
        var item = service.Register(new(recognitionLine.Id, ForeignMonetaryItemKind.Asset, "AR-EUR-001"),
            "subledger-accountant", "Register posted receivable");
        var settlementRate = rateService.CreateDraft(new(
            "EUR", new DateTime(2026, 1, 20), ExchangeRatePurpose.Transaction, 4.05m,
            "SAMA settlement rate", "evidence/EUR-settlement.pdf"), "rate-maker", "Settlement rate");
        rateService.Approve(settlementRate.Id, "rate-reviewer", "Settlement rate verified");
        var accounts = database.SeedFxSettlementAccounts();

        var result = service.Settle(new(
                item.Id, new DateTime(2026, 1, 20), database.PeriodId, 40m, settlementRate.Id,
                accounts.CashId, accounts.GainId, accounts.LossId),
            "settlement-maker", "settlement-reviewer", "settlement-poster", "Partial EUR receipt");

        Assert.Equal(ForeignMonetaryItemStatus.Open, result.Item.Status);
        Assert.Equal(60m, result.Item.OutstandingForeignAmount);
        Assert.Equal(240m, result.Item.CarryingAmountSar);
        Assert.Equal(160m, result.Settlement.CarryingAmountReleasedSar);
        Assert.Equal(162m, result.Settlement.SettlementAmountSar);
        Assert.Equal(2m, result.Settlement.RealizedGainLossSar);
        Assert.Throws<InvalidOperationException>(() => service.Settle(new(
                item.Id, new DateTime(2026, 1, 20), database.PeriodId, 61m, settlementRate.Id,
                accounts.CashId, accounts.GainId, accounts.LossId),
            "settlement-maker", "settlement-reviewer", "settlement-poster", "Over-settlement"));

        result.Settlement.ForeignAmount = 39m;
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void ForeignMonetaryItem_LiabilitySettlementPostsRealizedLoss()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var recognitionRate = rateService.CreateDraft(new(
            "GBP", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 4.50m,
            "SAMA recognition rate", "evidence/GBP-recognition.pdf"), "rate-maker", "Recognition rate");
        rateService.Approve(recognitionRate.Id, "rate-reviewer", "Recognition rate verified");
        var recognitionAccounts = database.SeedFxLiabilityRecognitionAccounts();
        var ledger = new GeneralLedgerService(database.Context);
        var recognition = ledger.CreateDraft(new JournalDraftRequest(
                new DateTime(2026, 1, 15), database.PeriodId, "GBP payable", "ForeignPurchase", "AP-GBP-001",
                new[]
                {
                    new JournalLineRequest(recognitionAccounts.ExpenseId, 450m, 0m),
                    new JournalLineRequest(recognitionAccounts.PayableId, 0m, 450m,
                        ForeignCurrencyCode: "GBP", ForeignAmount: 100m,
                        ForeignExchangeRateId: recognitionRate.Id)
                }),
            "purchase-accountant", "Foreign payable");
        ledger.Approve(recognition.Id, "purchase-reviewer", "Payable checked");
        ledger.Post(recognition.Id, "posting-controller", "Payable posted");
        var recognitionLine = database.Context.JournalEntryLines.Single(line =>
            line.JournalEntryId == recognition.Id && line.ForeignExchangeRateId != null);
        var service = new ForeignCurrencyMonetaryItemService(database.Context);
        var item = service.Register(new(recognitionLine.Id, ForeignMonetaryItemKind.Liability, "AP-GBP-001"),
            "subledger-accountant", "Register posted payable");
        var settlementRate = rateService.CreateDraft(new(
            "GBP", new DateTime(2026, 1, 22), ExchangeRatePurpose.Transaction, 4.55m,
            "SAMA settlement rate", "evidence/GBP-settlement.pdf"), "rate-maker", "Settlement rate");
        rateService.Approve(settlementRate.Id, "rate-reviewer", "Settlement rate verified");
        var accounts = database.SeedFxSettlementAccounts();

        var result = service.Settle(new(
                item.Id, new DateTime(2026, 1, 22), database.PeriodId, 100m, settlementRate.Id,
                accounts.CashId, accounts.GainId, accounts.LossId),
            "settlement-maker", "settlement-reviewer", "settlement-poster", "GBP payable settled");
        var lines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntryId == result.JournalEntry.Id).ToArray();

        Assert.Equal(-5m, result.Settlement.RealizedGainLossSar);
        Assert.Contains(lines, line => line.LedgerAccountId == recognitionAccounts.PayableId && line.Debit == 450m);
        Assert.Contains(lines, line => line.LedgerAccountId == accounts.CashId && line.Credit == 455m);
        Assert.Contains(lines, line => line.LedgerAccountId == accounts.LossId && line.Debit == 5m);
    }

    [Fact]
    public void ForeignCurrencyRevaluation_AssetLossFeedsLaterRealizedGainWithoutDuplication()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var recognitionRate = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 3.75m,
            "SAMA recognition rate", "evidence/USD-recognition-reval.pdf"), "rate-maker", "Recognition rate");
        rateService.Approve(recognitionRate.Id, "rate-reviewer", "Recognition rate verified");
        var item = database.RegisterPostedForeignAsset(
            "USD", 100m, 375m, recognitionRate.Id, "AR-USD-REVAL-001");
        var service = new ForeignCurrencyMonetaryItemService(database.Context);
        Assert.Throws<InvalidOperationException>(() => new GeneralLedgerService(database.Context)
            .ClosePeriod(database.PeriodId, "period-controller", "Premature close"));
        var closingRate = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 31), ExchangeRatePurpose.Closing, 3.74m,
            "SAMA closing rate", "evidence/USD-closing.pdf"), "rate-maker", "Closing rate");
        rateService.Approve(closingRate.Id, "rate-reviewer", "Closing rate verified");
        var accounts = database.SeedFxSettlementAccounts();

        var revaluation = service.Revalue(new(
                item.Id, new DateTime(2026, 1, 31), database.PeriodId, closingRate.Id,
                accounts.GainId, accounts.LossId, "REVAL-USD-202601"),
            "revaluation-maker", "revaluation-reviewer", "revaluation-poster", "January closing revaluation");
        var revaluationLines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntryId == revaluation.JournalEntry!.Id).ToArray();

        Assert.Equal(374m, revaluation.Item.CarryingAmountSar);
        Assert.Equal(-1m, revaluation.Revaluation.UnrealizedGainLossSar);
        Assert.Contains(revaluationLines, line => line.LedgerAccountId == accounts.LossId && line.Debit == 1m);
        Assert.Contains(revaluationLines, line => line.LedgerAccountId == item.LedgerAccountId && line.Credit == 1m);
        Assert.Throws<InvalidOperationException>(() => service.Revalue(new(
                item.Id, new DateTime(2026, 1, 31), database.PeriodId, closingRate.Id,
                accounts.GainId, accounts.LossId),
            "revaluation-maker", "revaluation-reviewer", "revaluation-poster", "Duplicate revaluation"));
        Assert.Throws<InvalidOperationException>(() => new GeneralLedgerService(database.Context).Reverse(
            revaluation.JournalEntry!.Id, new DateTime(2026, 1, 31), "controller", "Generic reversal forbidden"));

        var settlementRate = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 31), ExchangeRatePurpose.Transaction, 3.80m,
            "SAMA transaction rate", "evidence/USD-transaction-0131.pdf"), "rate-maker", "Settlement rate");
        rateService.Approve(settlementRate.Id, "rate-reviewer", "Settlement rate verified");
        var settlement = service.Settle(new(
                item.Id, new DateTime(2026, 1, 31), database.PeriodId, 100m, settlementRate.Id,
                accounts.CashId, accounts.GainId, accounts.LossId),
            "settlement-maker", "settlement-reviewer", "settlement-poster", "Settle after revaluation");

        Assert.Equal(6m, settlement.Settlement.RealizedGainLossSar);
        Assert.Equal(5m, revaluation.Revaluation.UnrealizedGainLossSar
            + settlement.Settlement.RealizedGainLossSar);
        new GeneralLedgerService(database.Context).ClosePeriod(
            database.PeriodId, "period-controller", "All foreign items measured or settled");
    }

    [Fact]
    public void ForeignCurrencyRevaluation_LiabilityDecreasePostsUnrealizedGainAndIsImmutable()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var recognitionRate = rateService.CreateDraft(new(
            "GBP", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 4.50m,
            "SAMA recognition rate", "evidence/GBP-recognition-reval.pdf"), "rate-maker", "Recognition rate");
        rateService.Approve(recognitionRate.Id, "rate-reviewer", "Recognition rate verified");
        var recognitionAccounts = database.SeedFxLiabilityRecognitionAccounts();
        var ledger = new GeneralLedgerService(database.Context);
        var recognition = ledger.CreateDraft(new JournalDraftRequest(
                new DateTime(2026, 1, 15), database.PeriodId, "GBP payable revaluation", "ForeignPurchase", "AP-GBP-REVAL",
                new[]
                {
                    new JournalLineRequest(recognitionAccounts.ExpenseId, 450m, 0m),
                    new JournalLineRequest(recognitionAccounts.PayableId, 0m, 450m,
                        ForeignCurrencyCode: "GBP", ForeignAmount: 100m,
                        ForeignExchangeRateId: recognitionRate.Id)
                }),
            "purchase-accountant", "Foreign payable");
        ledger.Approve(recognition.Id, "purchase-reviewer", "Payable checked");
        ledger.Post(recognition.Id, "posting-controller", "Payable posted");
        var recognitionLine = database.Context.JournalEntryLines.Single(line =>
            line.JournalEntryId == recognition.Id && line.ForeignExchangeRateId != null);
        var service = new ForeignCurrencyMonetaryItemService(database.Context);
        var item = service.Register(new(recognitionLine.Id, ForeignMonetaryItemKind.Liability, "AP-GBP-REVAL"),
            "subledger-accountant", "Register posted payable");
        var closingRate = rateService.CreateDraft(new(
            "GBP", new DateTime(2026, 1, 31), ExchangeRatePurpose.Closing, 4.45m,
            "SAMA closing rate", "evidence/GBP-closing.pdf"), "rate-maker", "Closing rate");
        rateService.Approve(closingRate.Id, "rate-reviewer", "Closing rate verified");
        var accounts = database.SeedFxSettlementAccounts();

        var result = service.Revalue(new(
                item.Id, new DateTime(2026, 1, 31), database.PeriodId, closingRate.Id,
                accounts.GainId, accounts.LossId),
            "revaluation-maker", "revaluation-reviewer", "revaluation-poster", "January closing revaluation");
        var lines = database.Context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntryId == result.JournalEntry!.Id).ToArray();

        Assert.Equal(445m, result.Item.CarryingAmountSar);
        Assert.Equal(5m, result.Revaluation.UnrealizedGainLossSar);
        Assert.Contains(lines, line => line.LedgerAccountId == recognitionAccounts.PayableId && line.Debit == 5m);
        Assert.Contains(lines, line => line.LedgerAccountId == accounts.GainId && line.Credit == 5m);
        result.Revaluation.UnrealizedGainLossSar = 4m;
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void ForeignCurrencyBatchClose_IsAllOrNothingWhenAClosingRateIsMissing()
    {
        using var database = LedgerTestDatabase.Create();
        var rateService = new ForeignExchangeRateService(database.Context);
        var usdTransaction = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 3.75m,
            "SAMA USD transaction", "evidence/USD-batch-transaction.pdf"), "rate-maker", "USD transaction");
        rateService.Approve(usdTransaction.Id, "rate-reviewer", "USD rate verified");
        var eurTransaction = rateService.CreateDraft(new(
            "EUR", new DateTime(2026, 1, 15), ExchangeRatePurpose.Transaction, 4m,
            "SAMA EUR transaction", "evidence/EUR-batch-transaction.pdf"), "rate-maker", "EUR transaction");
        rateService.Approve(eurTransaction.Id, "rate-reviewer", "EUR rate verified");
        var usdItem = database.RegisterPostedForeignAsset("USD", 100m, 375m, usdTransaction.Id, "AR-USD-BATCH");
        var eurItem = database.RegisterPostedForeignAsset("EUR", 100m, 400m, eurTransaction.Id, "AR-EUR-BATCH");
        var usdClosing = rateService.CreateDraft(new(
            "USD", new DateTime(2026, 1, 31), ExchangeRatePurpose.Closing, 3.74m,
            "SAMA USD closing", "evidence/USD-batch-closing.pdf"), "rate-maker", "USD closing");
        rateService.Approve(usdClosing.Id, "rate-reviewer", "USD closing verified");
        var accounts = database.SeedFxSettlementAccounts();
        var service = new ForeignCurrencyMonetaryItemService(database.Context);

        Assert.Throws<InvalidOperationException>(() => service.RevalueAllOpenItems(
            database.PeriodId, accounts.GainId, accounts.LossId,
            "SYSTEM:FX", "close-reviewer", "close-poster", "January batch close"));
        Assert.Empty(database.Context.ForeignCurrencyRevaluations);
        Assert.Equal(375m, database.Context.ForeignMonetaryItems.AsNoTracking().Single(value => value.Id == usdItem.Id).CarryingAmountSar);
        Assert.Equal(400m, database.Context.ForeignMonetaryItems.AsNoTracking().Single(value => value.Id == eurItem.Id).CarryingAmountSar);

        var eurClosing = rateService.CreateDraft(new(
            "EUR", new DateTime(2026, 1, 31), ExchangeRatePurpose.Closing, 4.02m,
            "SAMA EUR closing", "evidence/EUR-batch-closing.pdf"), "rate-maker", "EUR closing");
        rateService.Approve(eurClosing.Id, "rate-reviewer", "EUR closing verified");
        var results = service.RevalueAllOpenItems(
            database.PeriodId, accounts.GainId, accounts.LossId,
            "SYSTEM:FX", "close-reviewer", "close-poster", "January batch close");

        Assert.Equal(2, results.Count);
        Assert.Equal(374m, results.Single(value => value.Item.Id == usdItem.Id).Item.CarryingAmountSar);
        Assert.Equal(402m, results.Single(value => value.Item.Id == eurItem.Id).Item.CarryingAmountSar);
    }

    [Fact]
    public void AccountingManagementForm_ExposesGovernedAccountingWorkspacesForAccountant()
    {
        using var database = LedgerTestDatabase.Create();
        var userField = typeof(AuthenticationService).GetField("_currentUser", BindingFlags.Static | BindingFlags.NonPublic)!;
        var activityField = typeof(AuthenticationService).GetField("_lastActivityTime", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previousUser = userField.GetValue(null);
        var previousActivity = activityField.GetValue(null);
        try
        {
            userField.SetValue(null, new User
            {
                Username = "accounting-ui-test", FullName = "Accounting UI Test",
                Role = UserRole.Accountant, IsActive = true
            });
            activityField.SetValue(null, DateTime.Now);
            using var form = new AccountingManagementForm(database.Context);
            var tabs = form.Controls.OfType<TabControl>().Single();
            var names = tabs.TabPages.Cast<TabPage>().Select(page => page.Text).ToArray();

            Assert.Equal("إدارة المحاسبة", form.Text);
            Assert.Contains("القيود", names);
            Assert.Contains("الفترات المالية", names);
            Assert.Contains("السنوات المالية", names);
            Assert.Contains("التسويات", names);
            Assert.Contains("الترحيل التشغيلي", names);
            Assert.Contains("الإعداد المحاسبي", names);
            Assert.Contains("أسعار الصرف", names);
            Assert.Contains("البنود والعملات الأجنبية", names);
            using var movementForm = new StockMovementForm(database.Context);
            var movementButtons = Descendants(movementForm).OfType<Button>().Select(value => value.Text).ToArray();
            Assert.Contains("حفظ مسودة", movementButtons);
            Assert.Contains("اعتماد وتطبيق", movementButtons);
        }
        finally
        {
            userField.SetValue(null, previousUser);
            activityField.SetValue(null, previousActivity);
        }
    }

    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>()
        .SelectMany(control => new[] { control }.Concat(Descendants(control)));

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

        public (int CashId, int GainId, int LossId) SeedFxSettlementAccounts()
        {
            var cash = new LedgerAccount
            {
                Code = $"FXC-{Guid.NewGuid():N}", NameAr = "نقدية تسوية عملة",
                Type = LedgerAccountType.Asset, NormalBalance = AccountNormalBalance.Debit,
                FinancialStatementCategory = FinancialStatementCategory.Cash
            };
            var gain = new LedgerAccount
            {
                Code = $"FXG-{Guid.NewGuid():N}", NameAr = "أرباح فروق عملة محققة",
                Type = LedgerAccountType.Revenue, NormalBalance = AccountNormalBalance.Credit,
                FinancialStatementCategory = FinancialStatementCategory.OtherRevenue
            };
            var loss = new LedgerAccount
            {
                Code = $"FXL-{Guid.NewGuid():N}", NameAr = "خسائر فروق عملة محققة",
                Type = LedgerAccountType.Expense, NormalBalance = AccountNormalBalance.Debit,
                FinancialStatementCategory = FinancialStatementCategory.OtherOperatingExpense
            };
            Context.AddRange(cash, gain, loss);
            Context.SaveChanges();
            return (cash.Id, gain.Id, loss.Id);
        }

        public (int PayableId, int ExpenseId) SeedFxLiabilityRecognitionAccounts()
        {
            var payable = new LedgerAccount
            {
                Code = $"FXP-{Guid.NewGuid():N}", NameAr = "ذمم دائنة أجنبية",
                Type = LedgerAccountType.Liability, NormalBalance = AccountNormalBalance.Credit,
                FinancialStatementCategory = FinancialStatementCategory.AccountsPayable
            };
            var expense = new LedgerAccount
            {
                Code = $"FXE-{Guid.NewGuid():N}", NameAr = "مشتريات أجنبية",
                Type = LedgerAccountType.Expense, NormalBalance = AccountNormalBalance.Debit,
                FinancialStatementCategory = FinancialStatementCategory.CostOfGoodsSold
            };
            Context.AddRange(payable, expense);
            Context.SaveChanges();
            return (payable.Id, expense.Id);
        }

        public ForeignMonetaryItem RegisterPostedForeignAsset(
            string currencyCode,
            decimal foreignAmount,
            decimal sarAmount,
            long exchangeRateId,
            string reference)
        {
            var baseRequest = BalancedRequest(sarAmount);
            var request = baseRequest with
            {
                Lines = new[]
                {
                    baseRequest.Lines[0] with
                    {
                        ForeignCurrencyCode = currencyCode,
                        ForeignAmount = foreignAmount,
                        ForeignExchangeRateId = exchangeRateId
                    },
                    baseRequest.Lines[1]
                }
            };
            var ledger = new GeneralLedgerService(Context);
            var recognition = ledger.CreateDraft(request, "recognition-maker", "Foreign asset recognition");
            ledger.Approve(recognition.Id, "recognition-reviewer", "Recognition checked");
            ledger.Post(recognition.Id, "recognition-poster", "Recognition posted");
            var line = Context.JournalEntryLines.Single(value =>
                value.JournalEntryId == recognition.Id && value.ForeignExchangeRateId != null);
            return new ForeignCurrencyMonetaryItemService(Context).Register(
                new ForeignMonetaryItemRegistrationRequest(line.Id, ForeignMonetaryItemKind.Asset, reference),
                "subledger-accountant", "Register posted foreign asset");
        }

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

        public int SeedApprovedStockMovement(StockMovementType type, decimal amount)
        {
            var item = new InventoryItem
            {
                Name = $"Accounting inventory {Guid.NewGuid():N}", Category = InventoryCategory.Feed,
                Unit = "kg", CurrentStock = 100m, MinimumStock = 0m, MaximumStock = 1_000m,
                UnitCost = 10m, Status = InventoryStatus.Active, CreatedAt = DateTime.UtcNow
            };
            var movement = new StockMovement
            {
                InventoryItem = item, MovementDate = new DateTime(2026, 1, 20), MovementType = type,
                Quantity = amount / 10m, UnitCost = 10m, TotalCost = amount,
                Reference = $"MOV-{Guid.NewGuid():N}", IsApproved = true,
                CreatedAt = DateTime.UtcNow, CreatedBy = "inventory-approver"
            };
            Context.Add(movement);
            Context.SaveChanges();
            return movement.Id;
        }

        public int SeedSubmittedVatReturn(decimal outputVat, decimal inputVat, string periodNumber)
        {
            var user = new User
            {
                Username = $"vat-{Guid.NewGuid():N}", PasswordHash = "test-hash", FullName = "VAT Test User",
                Role = UserRole.Accountant, IsActive = true, CreatedBy = "test"
            };
            var vatReturn = new VATReturn
            {
                PeriodNumber = periodNumber, PeriodStartDate = new DateTime(2026, 1, 1),
                PeriodEndDate = new DateTime(2026, 1, 31), DueDate = new DateTime(2026, 2, 28),
                SubmissionDate = new DateTime(2026, 1, 31), Status = VATReturnStatus.Submitted,
                TaxRegistrationNumber = "123456789012345", Box6_VATOnSales = outputVat,
                Box10_VATOnPurchases = inputVat, Box11_NetVATDue = outputVat - inputVat,
                Box13_TotalVATDue = outputVat - inputVat, Box15_NetVATDueForPeriod = outputVat - inputVat,
                CreatedBy = user, CreatedDate = DateTime.UtcNow
            };
            Context.Add(vatReturn);
            Context.SaveChanges();
            return vatReturn.Id;
        }

        public int SeedPurchaseReceivingForInventory(decimal secondUnitPrice)
        {
            var supplier = new Supplier
            {
                Name = "Atomic Receiving Supplier", Type = SupplierType.Feed,
                Status = SupplierStatus.Active, CreatedAt = DateTime.UtcNow
            };
            var firstInventory = new InventoryItem
            {
                Name = "Atomic receiving A", Category = InventoryCategory.Feed, Unit = "kg",
                CurrentStock = 0m, MinimumStock = 0m, MaximumStock = 1_000m,
                UnitCost = 0m, Status = InventoryStatus.Active, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            var secondInventory = new InventoryItem
            {
                Name = "Atomic receiving B", Category = InventoryCategory.Feed, Unit = "kg",
                CurrentStock = 0m, MinimumStock = 0m, MaximumStock = 1_000m,
                UnitCost = 0m, Status = InventoryStatus.Active, IsActive = true, CreatedAt = DateTime.UtcNow
            };
            var order = new PurchaseOrder
            {
                OrderNumber = $"PO-ATOMIC-{Guid.NewGuid():N}", OrderDate = new DateTime(2026, 1, 10),
                ExpectedDeliveryDate = new DateTime(2026, 1, 20), Supplier = supplier,
                Status = PurchaseOrderStatus.Approved, SubTotal = 110m, AmountAfterDiscount = 110m,
                Total = 110m, CreatedAt = DateTime.UtcNow
            };
            var firstOrderItem = new PurchaseOrderItem
            {
                PurchaseOrder = order, InventoryItem = firstInventory, ItemName = firstInventory.Name,
                Quantity = 5m, RemainingQuantity = 5m, UnitPrice = 10m, TotalPrice = 50m,
                CreatedAt = DateTime.UtcNow
            };
            var secondOrderItem = new PurchaseOrderItem
            {
                PurchaseOrder = order, InventoryItem = secondInventory, ItemName = secondInventory.Name,
                Quantity = 3m, RemainingQuantity = 3m, UnitPrice = 20m, TotalPrice = 60m,
                CreatedAt = DateTime.UtcNow
            };
            var receiving = new PurchaseReceiving
            {
                ReceivingNumber = $"GRN-ATOMIC-{Guid.NewGuid():N}", PurchaseOrder = order,
                ReceivingDate = new DateTime(2026, 1, 20), IsFullReceiving = true,
                QualityInspectionCompleted = true, OverallQualityResult = QualityTestResult.Passed,
                InspectedBy = "quality-controller", InspectionDate = DateTime.UtcNow,
                ReceivedBy = "receiving-maker", CreatedBy = "receiving-maker", CreatedAt = DateTime.UtcNow,
                Items = new List<PurchaseReceivingItem>
                {
                    new()
                    {
                        PurchaseOrderItem = firstOrderItem, InventoryItem = firstInventory,
                        ItemName = firstInventory.Name, OrderedQuantity = 5m, ReceivedQuantity = 5m,
                        UnitPrice = 10m, QualityAccepted = true, QualityResult = QualityTestResult.Passed,
                        BatchNumber = "BATCH-A", CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        PurchaseOrderItem = secondOrderItem, InventoryItem = secondInventory,
                        ItemName = secondInventory.Name, OrderedQuantity = 3m, ReceivedQuantity = 3m,
                        UnitPrice = secondUnitPrice, QualityAccepted = true, QualityResult = QualityTestResult.Passed,
                        BatchNumber = "BATCH-B", CreatedAt = DateTime.UtcNow
                    }
                }
            };
            Context.Add(receiving);
            Context.SaveChanges();
            return receiving.Id;
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
