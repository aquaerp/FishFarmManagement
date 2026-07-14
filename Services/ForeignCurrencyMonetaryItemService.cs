using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record ForeignMonetaryItemRegistrationRequest(
    long PostedJournalEntryLineId,
    ForeignMonetaryItemKind Kind,
    string Reference);

public sealed record ForeignCurrencySettlementRequest(
    long ForeignMonetaryItemId,
    DateTime SettlementDate,
    int FiscalPeriodId,
    decimal ForeignAmount,
    long ForeignExchangeRateId,
    int CashAccountId,
    int RealizedGainAccountId,
    int RealizedLossAccountId,
    string? Reference = null);

public sealed record ForeignCurrencySettlementResult(
    ForeignMonetaryItem Item,
    ForeignCurrencySettlement Settlement,
    JournalEntry JournalEntry);

public sealed record ForeignCurrencyRevaluationRequest(
    long ForeignMonetaryItemId,
    DateTime RevaluationDate,
    int FiscalPeriodId,
    long ClosingExchangeRateId,
    int UnrealizedGainAccountId,
    int UnrealizedLossAccountId,
    string? Reference = null);

public sealed record ForeignCurrencyRevaluationResult(
    ForeignMonetaryItem Item,
    ForeignCurrencyRevaluation Revaluation,
    JournalEntry? JournalEntry);

public sealed class ForeignCurrencyMonetaryItemService
{
    private readonly FishFarmContext _context;
    private readonly GeneralLedgerService _ledger;

    public ForeignCurrencyMonetaryItemService(FishFarmContext context)
    {
        _context = context;
        _ledger = new GeneralLedgerService(context);
    }

    public ForeignMonetaryItem Register(
        ForeignMonetaryItemRegistrationRequest request,
        string actorUsername,
        string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        if (string.IsNullOrWhiteSpace(request.Reference))
            throw new ArgumentException("A monetary-item reference is required.", nameof(request));
        var line = _context.JournalEntryLines
            .Include(item => item.JournalEntry)
            .Include(item => item.LedgerAccount)
            .SingleOrDefault(item => item.Id == request.PostedJournalEntryLineId)
            ?? throw new InvalidOperationException("The recognition journal line does not exist.");
        if (line.JournalEntry.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("A foreign monetary item must originate from a posted journal line.");
        if (string.IsNullOrWhiteSpace(line.ForeignCurrencyCode)
            || !line.ForeignAmount.HasValue || !line.ForeignExchangeRateId.HasValue
            || !line.ExchangeRateSarPerUnit.HasValue)
            throw new InvalidOperationException("The recognition line has no governed foreign-currency measurement.");
        var validDirection = request.Kind switch
        {
            ForeignMonetaryItemKind.Asset => line.LedgerAccount.Type == LedgerAccountType.Asset && line.Debit > 0m,
            ForeignMonetaryItemKind.Liability => line.LedgerAccount.Type == LedgerAccountType.Liability && line.Credit > 0m,
            _ => false
        };
        if (!validDirection)
            throw new InvalidOperationException("The recognition line direction and account type do not match the monetary-item kind.");

        var carryingAmount = line.Debit + line.Credit;
        var item = new ForeignMonetaryItem
        {
            Reference = request.Reference.Trim(),
            Kind = request.Kind,
            RecognitionJournalEntryLineId = line.Id,
            LedgerAccountId = line.LedgerAccountId,
            CurrencyCode = line.ForeignCurrencyCode,
            OriginalForeignAmount = line.ForeignAmount.Value,
            OutstandingForeignAmount = line.ForeignAmount.Value,
            CarryingAmountSar = carryingAmount,
            LastMeasurementDate = line.JournalEntry.EntryDate,
            Status = ForeignMonetaryItemStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actorUsername.Trim()
        };
        _context.ForeignMonetaryItems.Add(item);
        _context.SaveChanges();
        AddAudit(nameof(ForeignMonetaryItem), item.Id, "Register", actorUsername, reason, null, Snapshot(item));
        _context.SaveChanges();
        return item;
    }

    public ForeignCurrencySettlementResult Settle(
        ForeignCurrencySettlementRequest request,
        string journalCreator,
        string journalApprover,
        string journalPoster,
        string reason)
    {
        RequireActorAndReason(journalCreator, reason);
        RequireActorAndReason(journalApprover, reason);
        RequireActorAndReason(journalPoster, reason);
        if (string.Equals(journalCreator.Trim(), journalApprover.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The settlement journal creator cannot approve the same journal.");
        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var item = _context.ForeignMonetaryItems.SingleOrDefault(value => value.Id == request.ForeignMonetaryItemId)
            ?? throw new InvalidOperationException("The foreign monetary item does not exist.");
        if (item.Status != ForeignMonetaryItemStatus.Open || item.OutstandingForeignAmount <= 0m)
            throw new InvalidOperationException("Only an open foreign monetary item can be settled.");
        if (request.ForeignAmount <= 0m || request.ForeignAmount > item.OutstandingForeignAmount
            || DecimalScale(request.ForeignAmount) > 8)
            throw new InvalidOperationException("The settlement foreign amount is invalid or exceeds the outstanding balance.");
        if (request.SettlementDate.Date < item.LastMeasurementDate.Date)
            throw new InvalidOperationException("Settlement cannot precede the item's last measurement date.");

        var rate = _context.ForeignExchangeRates.AsNoTracking().SingleOrDefault(value =>
            value.Id == request.ForeignExchangeRateId
            && value.Status == ExchangeRateStatus.Approved
            && value.Purpose == ExchangeRatePurpose.Transaction
            && value.CurrencyCode == item.CurrencyCode
            && value.RateDate == request.SettlementDate.Date)
            ?? throw new InvalidOperationException("An approved transaction rate for the settlement date and currency is required.");
        ValidateSettlementAccounts(item, request);

        var isFinal = request.ForeignAmount == item.OutstandingForeignAmount;
        var carryingReleased = isFinal
            ? item.CarryingAmountSar
            : decimal.Round(item.CarryingAmountSar * request.ForeignAmount / item.OutstandingForeignAmount,
                2, MidpointRounding.AwayFromZero);
        var settlementSar = decimal.Round(request.ForeignAmount * rate.SarPerUnit, 2, MidpointRounding.AwayFromZero);
        var realizedGainLoss = item.Kind == ForeignMonetaryItemKind.Asset
            ? settlementSar - carryingReleased
            : carryingReleased - settlementSar;
        var lines = BuildSettlementLines(item, request, rate, carryingReleased, settlementSar, realizedGainLoss);
        var journal = _ledger.CreateDraft(new JournalDraftRequest(
                request.SettlementDate,
                request.FiscalPeriodId,
                $"Foreign currency settlement {item.Reference}",
                "ForeignCurrencySettlement",
                string.IsNullOrWhiteSpace(request.Reference) ? item.Reference : request.Reference.Trim(),
                lines),
            journalCreator, reason);
        _ledger.Approve(journal.Id, journalApprover, reason);
        journal = _ledger.Post(journal.Id, journalPoster, reason);

        var before = Snapshot(item);
        item.OutstandingForeignAmount = isFinal ? 0m : item.OutstandingForeignAmount - request.ForeignAmount;
        item.CarryingAmountSar = isFinal ? 0m : item.CarryingAmountSar - carryingReleased;
        item.LastMeasurementDate = request.SettlementDate.Date;
        item.Status = isFinal ? ForeignMonetaryItemStatus.Settled : ForeignMonetaryItemStatus.Open;
        var settlement = new ForeignCurrencySettlement
        {
            ForeignMonetaryItemId = item.Id,
            SettlementDate = request.SettlementDate.Date,
            ForeignAmount = request.ForeignAmount,
            CarryingAmountReleasedSar = carryingReleased,
            SettlementAmountSar = settlementSar,
            RealizedGainLossSar = realizedGainLoss,
            ForeignExchangeRateId = rate.Id,
            JournalEntryId = journal.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = journalCreator.Trim()
        };
        _context.ForeignCurrencySettlements.Add(settlement);
        _context.SaveChanges();
        AddAudit(nameof(ForeignMonetaryItem), item.Id, "Settle", journalPoster, reason, before, Snapshot(item));
        AddAudit(nameof(ForeignCurrencySettlement), settlement.Id, "Create", journalPoster, reason, null,
            JsonSerializer.Serialize(new
            {
                settlement.ForeignMonetaryItemId,
                settlement.SettlementDate,
                settlement.ForeignAmount,
                settlement.CarryingAmountReleasedSar,
                settlement.SettlementAmountSar,
                settlement.RealizedGainLossSar,
                settlement.ForeignExchangeRateId,
                settlement.JournalEntryId
            }));
        _context.SaveChanges();
        transaction?.Commit();
        return new ForeignCurrencySettlementResult(item, settlement, journal);
    }

    public ForeignCurrencyRevaluationResult Revalue(
        ForeignCurrencyRevaluationRequest request,
        string journalCreator,
        string journalApprover,
        string journalPoster,
        string reason)
    {
        RequireActorAndReason(journalCreator, reason);
        RequireActorAndReason(journalApprover, reason);
        RequireActorAndReason(journalPoster, reason);
        if (string.Equals(journalCreator.Trim(), journalApprover.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The revaluation journal creator cannot approve the same journal.");
        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var item = _context.ForeignMonetaryItems.SingleOrDefault(value => value.Id == request.ForeignMonetaryItemId)
            ?? throw new InvalidOperationException("The foreign monetary item does not exist.");
        if (item.Status != ForeignMonetaryItemStatus.Open || item.OutstandingForeignAmount <= 0m)
            throw new InvalidOperationException("Only an open foreign monetary item can be revalued.");
        if (request.RevaluationDate.Date < item.LastMeasurementDate.Date)
            throw new InvalidOperationException("Revaluation cannot precede the item's last measurement date.");
        if (_context.ForeignCurrencyRevaluations.Any(value => value.ForeignMonetaryItemId == item.Id
            && value.RevaluationDate == request.RevaluationDate.Date))
            throw new InvalidOperationException("The monetary item has already been revalued for this date.");
        var rate = _context.ForeignExchangeRates.AsNoTracking().SingleOrDefault(value =>
            value.Id == request.ClosingExchangeRateId
            && value.Status == ExchangeRateStatus.Approved
            && value.Purpose == ExchangeRatePurpose.Closing
            && value.CurrencyCode == item.CurrencyCode
            && value.RateDate == request.RevaluationDate.Date)
            ?? throw new InvalidOperationException("An approved closing rate for the revaluation date and currency is required.");
        ValidateDifferenceAccounts(request.UnrealizedGainAccountId, request.UnrealizedLossAccountId);

        var previousCarrying = item.CarryingAmountSar;
        var revaluedCarrying = decimal.Round(item.OutstandingForeignAmount * rate.SarPerUnit,
            2, MidpointRounding.AwayFromZero);
        var unrealizedGainLoss = item.Kind == ForeignMonetaryItemKind.Asset
            ? revaluedCarrying - previousCarrying
            : previousCarrying - revaluedCarrying;
        JournalEntry? journal = null;
        if (unrealizedGainLoss != 0m)
        {
            var lines = BuildRevaluationLines(item, request, previousCarrying, revaluedCarrying);
            journal = _ledger.CreateDraft(new JournalDraftRequest(
                    request.RevaluationDate,
                    request.FiscalPeriodId,
                    $"Foreign currency closing revaluation {item.Reference}",
                    "ForeignCurrencyRevaluation",
                    string.IsNullOrWhiteSpace(request.Reference) ? item.Reference : request.Reference.Trim(),
                    lines),
                journalCreator, reason);
            _ledger.Approve(journal.Id, journalApprover, reason);
            journal = _ledger.Post(journal.Id, journalPoster, reason);
        }

        var before = Snapshot(item);
        item.CarryingAmountSar = revaluedCarrying;
        item.LastMeasurementDate = request.RevaluationDate.Date;
        var revaluation = new ForeignCurrencyRevaluation
        {
            ForeignMonetaryItemId = item.Id,
            RevaluationDate = request.RevaluationDate.Date,
            PreviousCarryingAmountSar = previousCarrying,
            RevaluedCarryingAmountSar = revaluedCarrying,
            UnrealizedGainLossSar = unrealizedGainLoss,
            ForeignExchangeRateId = rate.Id,
            JournalEntryId = journal?.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = journalCreator.Trim()
        };
        _context.ForeignCurrencyRevaluations.Add(revaluation);
        _context.SaveChanges();
        AddAudit(nameof(ForeignMonetaryItem), item.Id, "Revalue", journalPoster, reason, before, Snapshot(item));
        AddAudit(nameof(ForeignCurrencyRevaluation), revaluation.Id, "Create", journalPoster, reason, null,
            JsonSerializer.Serialize(new
            {
                revaluation.ForeignMonetaryItemId,
                revaluation.RevaluationDate,
                revaluation.PreviousCarryingAmountSar,
                revaluation.RevaluedCarryingAmountSar,
                revaluation.UnrealizedGainLossSar,
                revaluation.ForeignExchangeRateId,
                revaluation.JournalEntryId
            }));
        _context.SaveChanges();
        transaction?.Commit();
        return new ForeignCurrencyRevaluationResult(item, revaluation, journal);
    }

    public IReadOnlyList<ForeignCurrencyRevaluationResult> RevalueAllOpenItems(
        int fiscalPeriodId,
        int unrealizedGainAccountId,
        int unrealizedLossAccountId,
        string journalCreator,
        string journalApprover,
        string journalPoster,
        string reason)
    {
        RequireActorAndReason(journalCreator, reason);
        var period = _context.FiscalPeriods.AsNoTracking().SingleOrDefault(value => value.Id == fiscalPeriodId)
            ?? throw new InvalidOperationException("The fiscal period does not exist.");
        if (period.Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException("Foreign currency closing revaluation requires an open fiscal period.");
        var closingDate = period.EndDate.Date;
        var items = _context.ForeignMonetaryItems
            .Where(item => item.Status == ForeignMonetaryItemStatus.Open
                && item.RecognitionJournalEntryLine.JournalEntry.EntryDate <= closingDate
                && item.LastMeasurementDate != closingDate)
            .OrderBy(item => item.CurrencyCode).ThenBy(item => item.Reference).ToArray();
        if (items.Length == 0) return Array.Empty<ForeignCurrencyRevaluationResult>();
        var currencies = items.Select(item => item.CurrencyCode).Distinct().ToArray();
        var rates = _context.ForeignExchangeRates.AsNoTracking().Where(rate =>
                currencies.Contains(rate.CurrencyCode)
                && rate.RateDate == closingDate
                && rate.Purpose == ExchangeRatePurpose.Closing
                && rate.Status == ExchangeRateStatus.Approved)
            .ToDictionary(rate => rate.CurrencyCode);
        var missing = currencies.Where(currency => !rates.ContainsKey(currency)).ToArray();
        if (missing.Length != 0)
            throw new InvalidOperationException($"Approved closing rates are missing for: {string.Join(", ", missing)}.");

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var results = new List<ForeignCurrencyRevaluationResult>(items.Length);
        foreach (var item in items)
        {
            results.Add(Revalue(new ForeignCurrencyRevaluationRequest(
                    item.Id, closingDate, fiscalPeriodId, rates[item.CurrencyCode].Id,
                    unrealizedGainAccountId, unrealizedLossAccountId,
                    $"FX-CLOSE-{closingDate:yyyyMMdd}-{item.Reference}"),
                journalCreator, journalApprover, journalPoster, reason));
        }
        transaction.Commit();
        return results;
    }

    private void ValidateSettlementAccounts(ForeignMonetaryItem item, ForeignCurrencySettlementRequest request)
    {
        var ids = new[] { request.CashAccountId, request.RealizedGainAccountId, request.RealizedLossAccountId }.Distinct().ToArray();
        if (ids.Length != 3) throw new InvalidOperationException("Cash, realized gain, and realized loss accounts must be distinct.");
        var accounts = _context.LedgerAccounts.AsNoTracking().Where(account => ids.Contains(account.Id))
            .ToDictionary(account => account.Id);
        if (accounts.Count != 3
            || accounts[request.CashAccountId].Type != LedgerAccountType.Asset
            || accounts[request.RealizedGainAccountId].Type != LedgerAccountType.Revenue
            || accounts[request.RealizedLossAccountId].Type != LedgerAccountType.Expense
            || accounts.Values.Any(account => !account.IsActive || !account.AllowsPosting
                || account.CurrencyCode != GeneralLedgerService.FunctionalCurrencyCode))
            throw new InvalidOperationException("Settlement accounts must be active SAR cash, revenue-gain, and expense-loss posting accounts.");
        if (item.LedgerAccountId == request.CashAccountId)
            throw new InvalidOperationException("The settlement cash account cannot be the monetary item's ledger account.");
    }

    private void ValidateDifferenceAccounts(int gainAccountId, int lossAccountId)
    {
        if (gainAccountId == lossAccountId)
            throw new InvalidOperationException("Unrealized gain and loss accounts must be distinct.");
        var ids = new[] { gainAccountId, lossAccountId };
        var accounts = _context.LedgerAccounts.AsNoTracking().Where(account => ids.Contains(account.Id))
            .ToDictionary(account => account.Id);
        if (accounts.Count != 2
            || accounts[gainAccountId].Type != LedgerAccountType.Revenue
            || accounts[lossAccountId].Type != LedgerAccountType.Expense
            || accounts.Values.Any(account => !account.IsActive || !account.AllowsPosting
                || account.CurrencyCode != GeneralLedgerService.FunctionalCurrencyCode))
            throw new InvalidOperationException("Revaluation accounts must be active SAR revenue-gain and expense-loss posting accounts.");
    }

    private static IReadOnlyList<JournalLineRequest> BuildSettlementLines(
        ForeignMonetaryItem item,
        ForeignCurrencySettlementRequest request,
        ForeignExchangeRate rate,
        decimal carryingReleased,
        decimal settlementSar,
        decimal realizedGainLoss)
    {
        var lines = new List<JournalLineRequest>();
        if (item.Kind == ForeignMonetaryItemKind.Asset)
        {
            lines.Add(new JournalLineRequest(request.CashAccountId, settlementSar, 0m, Description: "Foreign cash received",
                ForeignCurrencyCode: item.CurrencyCode, ForeignAmount: request.ForeignAmount,
                ForeignExchangeRateId: rate.Id));
            lines.Add(new JournalLineRequest(item.LedgerAccountId, 0m, carryingReleased, Description: "Derecognize foreign monetary asset"));
        }
        else
        {
            lines.Add(new JournalLineRequest(item.LedgerAccountId, carryingReleased, 0m, Description: "Derecognize foreign monetary liability"));
            lines.Add(new JournalLineRequest(request.CashAccountId, 0m, settlementSar, Description: "Foreign cash paid",
                ForeignCurrencyCode: item.CurrencyCode, ForeignAmount: request.ForeignAmount,
                ForeignExchangeRateId: rate.Id));
        }

        if (realizedGainLoss > 0m)
            lines.Add(new JournalLineRequest(request.RealizedGainAccountId, 0m, realizedGainLoss, Description: "Realized foreign exchange gain"));
        else if (realizedGainLoss < 0m)
            lines.Add(new JournalLineRequest(request.RealizedLossAccountId, -realizedGainLoss, 0m, Description: "Realized foreign exchange loss"));
        return lines;
    }

    private static IReadOnlyList<JournalLineRequest> BuildRevaluationLines(
        ForeignMonetaryItem item,
        ForeignCurrencyRevaluationRequest request,
        decimal previousCarrying,
        decimal revaluedCarrying)
    {
        var difference = Math.Abs(revaluedCarrying - previousCarrying);
        var lines = new List<JournalLineRequest>();
        if (item.Kind == ForeignMonetaryItemKind.Asset)
        {
            if (revaluedCarrying > previousCarrying)
            {
                lines.Add(new JournalLineRequest(item.LedgerAccountId, difference, 0m, Description: "Increase foreign monetary asset at closing rate"));
                lines.Add(new JournalLineRequest(request.UnrealizedGainAccountId, 0m, difference, Description: "Unrealized foreign exchange gain"));
            }
            else
            {
                lines.Add(new JournalLineRequest(request.UnrealizedLossAccountId, difference, 0m, Description: "Unrealized foreign exchange loss"));
                lines.Add(new JournalLineRequest(item.LedgerAccountId, 0m, difference, Description: "Decrease foreign monetary asset at closing rate"));
            }
        }
        else if (revaluedCarrying > previousCarrying)
        {
            lines.Add(new JournalLineRequest(request.UnrealizedLossAccountId, difference, 0m, Description: "Unrealized foreign exchange loss"));
            lines.Add(new JournalLineRequest(item.LedgerAccountId, 0m, difference, Description: "Increase foreign monetary liability at closing rate"));
        }
        else
        {
            lines.Add(new JournalLineRequest(item.LedgerAccountId, difference, 0m, Description: "Decrease foreign monetary liability at closing rate"));
            lines.Add(new JournalLineRequest(request.UnrealizedGainAccountId, 0m, difference, Description: "Unrealized foreign exchange gain"));
        }
        return lines;
    }

    private void AddAudit(string entityType, long entityId, string action, string actor, string reason, string? before, string? after) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            BeforeJson = before,
            AfterJson = after,
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static string Snapshot(ForeignMonetaryItem item) => JsonSerializer.Serialize(new
    {
        item.Reference,
        item.Kind,
        item.RecognitionJournalEntryLineId,
        item.LedgerAccountId,
        item.CurrencyCode,
        item.OriginalForeignAmount,
        item.OutstandingForeignAmount,
        item.CarryingAmountSar,
        item.LastMeasurementDate,
        item.Status
    });

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }
}
