using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record JournalLineRequest(
    int LedgerAccountId,
    decimal Debit,
    decimal Credit,
    int? CostCenterId = null,
    string? Description = null,
    string? ForeignCurrencyCode = null,
    decimal? ForeignAmount = null,
    long? ForeignExchangeRateId = null);

public sealed record JournalDraftRequest(
    DateTime EntryDate,
    int FiscalPeriodId,
    string Description,
    string Source,
    string? Reference,
    IReadOnlyList<JournalLineRequest> Lines);

public static class JournalBalanceValidator
{
    public static (decimal Debit, decimal Credit) Validate(IEnumerable<JournalLineRequest> lines)
    {
        decimal totalDebit = 0;
        decimal totalCredit = 0;
        var count = 0;
        foreach (var line in lines)
        {
            count++;
            if (line.Debit < 0 || line.Credit < 0)
                throw new InvalidOperationException("Journal amounts cannot be negative.");
            if (decimal.Round(line.Debit, 2) != line.Debit || decimal.Round(line.Credit, 2) != line.Credit)
                throw new InvalidOperationException("Journal amounts cannot exceed two decimal places.");
            if ((line.Debit > 0) == (line.Credit > 0))
                throw new InvalidOperationException("Each journal line must contain either a debit or a credit amount.");
            totalDebit = checked(totalDebit + line.Debit);
            totalCredit = checked(totalCredit + line.Credit);
        }

        if (count < 2)
            throw new InvalidOperationException("A journal entry requires at least two lines.");
        if (totalDebit != totalCredit)
            throw new InvalidOperationException("The journal entry is not balanced.");
        return (totalDebit, totalCredit);
    }
}

public sealed class GeneralLedgerService
{
    public const string FunctionalCurrencyCode = "SAR";
    private const string JournalSequenceName = "JournalEntry";
    private readonly FishFarmContext _context;

    public GeneralLedgerService(FishFarmContext context) => _context = context;

    public JournalEntry CreateDraft(JournalDraftRequest request, string actorUsername, string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        if (string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Source))
            throw new InvalidOperationException("Journal description and source are required.");

        JournalBalanceValidator.Validate(request.Lines);
        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var period = _context.FiscalPeriods.Include(item => item.FiscalYear)
            .SingleOrDefault(item => item.Id == request.FiscalPeriodId)
            ?? throw new InvalidOperationException("The fiscal period does not exist.");
        EnsurePeriodIsOpen(period, request.EntryDate);
        var foreignMeasurements = ValidateForeignMeasurements(request.EntryDate, request.Lines);

        var accountIds = request.Lines.Select(line => line.LedgerAccountId).Distinct().ToArray();
        var accounts = _context.LedgerAccounts
            .Where(account => accountIds.Contains(account.Id) && account.IsActive && account.AllowsPosting)
            .Select(account => new { account.Id, account.CurrencyCode }).ToArray();
        if (accounts.Length != accountIds.Length)
            throw new InvalidOperationException("Every journal line must use an active posting account.");
        if (accounts.Any(account => !string.Equals(account.CurrencyCode, FunctionalCurrencyCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Only {FunctionalCurrencyCode} accounts can be posted until foreign-currency accounting is implemented.");

        var costCenterIds = request.Lines.Where(line => line.CostCenterId.HasValue)
            .Select(line => line.CostCenterId!.Value).Distinct().ToArray();
        if (costCenterIds.Length != 0)
        {
            var activeCount = _context.CostCenters.Count(center => costCenterIds.Contains(center.Id) && center.IsActive);
            if (activeCount != costCenterIds.Length)
                throw new InvalidOperationException("Every selected cost center must be active.");
        }

        var sequenceNumber = TakeNextSequence();
        var entry = new JournalEntry
        {
            SequenceNumber = sequenceNumber,
            EntryNumber = $"JE-{sequenceNumber:D10}",
            EntryDate = request.EntryDate.Date,
            FiscalPeriodId = request.FiscalPeriodId,
            Description = request.Description.Trim(),
            Source = request.Source.Trim(),
            Reference = NullIfWhiteSpace(request.Reference),
            Status = JournalEntryStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actorUsername.Trim(),
            Lines = request.Lines.Select((line, index) => new JournalEntryLine
            {
                LineNumber = index + 1,
                LedgerAccountId = line.LedgerAccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                CostCenterId = line.CostCenterId,
                Description = NullIfWhiteSpace(line.Description),
                ForeignCurrencyCode = foreignMeasurements[index]?.CurrencyCode,
                ForeignAmount = line.ForeignAmount,
                ForeignExchangeRateId = line.ForeignExchangeRateId,
                ExchangeRateSarPerUnit = foreignMeasurements[index]?.SarPerUnit
            }).ToList()
        };
        _context.JournalEntries.Add(entry);
        _context.SaveChanges();
        AddAudit(entry.Id, "CreateDraft", actorUsername, reason, null, Snapshot(entry));
        _context.SaveChanges();
        transaction?.Commit();
        return entry;
    }

    public JournalEntry Approve(long entryId, string actorUsername, string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        var entry = LoadEntry(entryId);
        if (entry.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Only draft entries can be approved.");
        if (string.Equals(entry.CreatedBy, actorUsername, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The creator cannot approve the same journal entry.");

        var before = Snapshot(entry);
        ValidatePersistedLines(entry.Lines);
        ValidatePersistedForeignMeasurements(entry);
        EnsureFunctionalCurrency(entry.Lines);
        EnsurePeriodIsOpen(entry.FiscalPeriod, entry.EntryDate);
        entry.Status = JournalEntryStatus.Approved;
        entry.ApprovedAtUtc = DateTime.UtcNow;
        entry.ApprovedBy = actorUsername.Trim();
        AddAudit(entry.Id, "Approve", actorUsername, reason, before, Snapshot(entry));
        _context.SaveChanges();
        return entry;
    }

    public JournalEntry Post(long entryId, string actorUsername, string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        var entry = LoadEntry(entryId);
        if (entry.Status != JournalEntryStatus.Approved)
            throw new InvalidOperationException("Only approved entries can be posted.");

        var before = Snapshot(entry);
        ValidatePersistedLines(entry.Lines);
        ValidatePersistedForeignMeasurements(entry);
        EnsureFunctionalCurrency(entry.Lines);
        EnsurePeriodIsOpen(entry.FiscalPeriod, entry.EntryDate);
        entry.Status = JournalEntryStatus.Posted;
        entry.PostedAtUtc = DateTime.UtcNow;
        entry.PostedBy = actorUsername.Trim();
        AddAudit(entry.Id, "Post", actorUsername, reason, before, Snapshot(entry));
        _context.SaveChanges();
        return entry;
    }

    public JournalEntry Reverse(long entryId, DateTime reversalDate, string actorUsername, string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var original = LoadEntry(entryId);
        if (original.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted entries can be reversed.");
        var originalLineIds = original.Lines.Select(line => line.Id).ToArray();
        if (_context.ForeignMonetaryItems.Any(item => originalLineIds.Contains(item.RecognitionJournalEntryLineId)))
            throw new InvalidOperationException("A journal that originates a governed foreign monetary item cannot be reversed through the generic reversal workflow.");
        if (_context.ForeignCurrencySettlements.Any(item => item.JournalEntryId == original.Id)
            || _context.ForeignCurrencyRevaluations.Any(item => item.JournalEntryId == original.Id))
            throw new InvalidOperationException("A governed foreign-currency settlement or revaluation cannot be reversed through the generic reversal workflow.");
        EnsureFunctionalCurrency(original.Lines);
        var reversalPeriod = _context.FiscalPeriods.Include(item => item.FiscalYear)
            .SingleOrDefault(item => item.StartDate <= reversalDate.Date && item.EndDate >= reversalDate.Date)
            ?? throw new InvalidOperationException("No fiscal period covers the reversal date.");
        EnsurePeriodIsOpen(reversalPeriod, reversalDate);

        var request = new JournalDraftRequest(
            reversalDate,
            reversalPeriod.Id,
            $"Reversal of {original.EntryNumber}: {original.Description}",
            "Reversal",
            original.EntryNumber,
            original.Lines.OrderBy(line => line.LineNumber)
                .Select(line => new JournalLineRequest(line.LedgerAccountId, line.Credit, line.Debit, line.CostCenterId,
                    line.Description, line.ForeignCurrencyCode, line.ForeignAmount, line.ForeignExchangeRateId))
                .ToArray());

        // Create within this transaction without opening a nested transaction.
        JournalBalanceValidator.Validate(request.Lines);
        var sequenceNumber = TakeNextSequence();
        var reversal = new JournalEntry
        {
            SequenceNumber = sequenceNumber,
            EntryNumber = $"JE-{sequenceNumber:D10}",
            EntryDate = reversalDate.Date,
            FiscalPeriodId = reversalPeriod.Id,
            Description = request.Description,
            Source = request.Source,
            Reference = request.Reference,
            Status = JournalEntryStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actorUsername.Trim(),
            ApprovedAtUtc = DateTime.UtcNow,
            ApprovedBy = actorUsername.Trim(),
            ReversalOfJournalEntryId = original.Id,
            Lines = request.Lines.Select((line, index) => new JournalEntryLine
            {
                LineNumber = index + 1,
                LedgerAccountId = line.LedgerAccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                CostCenterId = line.CostCenterId,
                Description = line.Description,
                ForeignCurrencyCode = line.ForeignCurrencyCode,
                ForeignAmount = line.ForeignAmount,
                ForeignExchangeRateId = line.ForeignExchangeRateId,
                ExchangeRateSarPerUnit = original.Lines.OrderBy(item => item.LineNumber).ElementAt(index).ExchangeRateSarPerUnit
            }).ToList()
        };
        var before = Snapshot(original);
        original.Status = JournalEntryStatus.Reversed;
        original.ReversedAtUtc = DateTime.UtcNow;
        original.ReversedBy = actorUsername.Trim();
        _context.JournalEntries.Add(reversal);
        _context.SaveChanges();
        reversal.Status = JournalEntryStatus.Posted;
        reversal.PostedAtUtc = DateTime.UtcNow;
        reversal.PostedBy = actorUsername.Trim();
        AddAudit(original.Id, "Reverse", actorUsername, reason, before, Snapshot(original));
        AddAudit(reversal.Id, "CreateReversal", actorUsername, reason, null, Snapshot(reversal));
        _context.SaveChanges();
        transaction?.Commit();
        return reversal;
    }

    public void ClosePeriod(int periodId, string actorUsername, string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        var period = _context.FiscalPeriods.SingleOrDefault(item => item.Id == periodId)
            ?? throw new InvalidOperationException("The fiscal period does not exist.");
        if (period.Status == FiscalPeriodStatus.Closed) return;
        if (_context.JournalEntries.Any(entry => entry.FiscalPeriodId == periodId
            && (entry.Status == JournalEntryStatus.Draft || entry.Status == JournalEntryStatus.Approved)))
            throw new InvalidOperationException("A fiscal period with unposted entries cannot be closed.");
        if (_context.ForeignMonetaryItems.Any(item => item.Status == ForeignMonetaryItemStatus.Open
            && item.RecognitionJournalEntryLine.JournalEntry.EntryDate <= period.EndDate.Date
            && item.LastMeasurementDate != period.EndDate.Date))
            throw new InvalidOperationException("All open foreign monetary items must be revalued at the period-end date before closing.");

        var before = JsonSerializer.Serialize(new { period.Status, period.ClosedAtUtc, period.ClosedBy });
        period.Status = FiscalPeriodStatus.Closed;
        period.ClosedAtUtc = DateTime.UtcNow;
        period.ClosedBy = actorUsername.Trim();
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(FiscalPeriod),
            EntityId = period.Id.ToString(),
            Action = "Close",
            ActorUsername = actorUsername.Trim(),
            Reason = reason.Trim(),
            BeforeJson = before,
            AfterJson = JsonSerializer.Serialize(new { period.Status, period.ClosedAtUtc, period.ClosedBy }),
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        _context.SaveChanges();
    }

    private JournalEntry LoadEntry(long entryId) =>
        _context.JournalEntries.Include(entry => entry.Lines)
            .Include(entry => entry.FiscalPeriod).ThenInclude(period => period.FiscalYear)
            .SingleOrDefault(entry => entry.Id == entryId)
        ?? throw new InvalidOperationException("The journal entry does not exist.");

    private long TakeNextSequence()
    {
        var sequence = _context.AccountingSequences.SingleOrDefault(item => item.Name == JournalSequenceName);
        if (sequence == null)
        {
            sequence = new AccountingSequence { Name = JournalSequenceName, NextValue = 2 };
            _context.AccountingSequences.Add(sequence);
            return 1;
        }
        var value = sequence.NextValue;
        sequence.NextValue = checked(value + 1);
        return value;
    }

    private static void EnsurePeriodIsOpen(FiscalPeriod period, DateTime entryDate)
    {
        if (period.Status != FiscalPeriodStatus.Open || period.FiscalYear?.IsClosed == true)
            throw new InvalidOperationException("Posting to a closed fiscal period is forbidden.");
        if (entryDate.Date < period.StartDate.Date || entryDate.Date > period.EndDate.Date)
            throw new InvalidOperationException("The journal date is outside the fiscal period.");
    }

    private static void ValidatePersistedLines(IEnumerable<JournalEntryLine> lines) =>
        JournalBalanceValidator.Validate(lines.Select(line =>
            new JournalLineRequest(line.LedgerAccountId, line.Debit, line.Credit, line.CostCenterId, line.Description)));

    private ForeignExchangeRate?[] ValidateForeignMeasurements(DateTime entryDate, IReadOnlyList<JournalLineRequest> lines)
    {
        var result = new ForeignExchangeRate?[lines.Count];
        var rateIds = lines.Where(HasAnyForeignMeasurement)
            .Select(line => line.ForeignExchangeRateId)
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var rates = _context.ForeignExchangeRates.AsNoTracking()
            .Where(rate => rateIds.Contains(rate.Id)).ToDictionary(rate => rate.Id);

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            if (!HasAnyForeignMeasurement(line)) continue;
            if (string.IsNullOrWhiteSpace(line.ForeignCurrencyCode)
                || !line.ForeignAmount.HasValue || !line.ForeignExchangeRateId.HasValue)
                throw new InvalidOperationException("Foreign currency code, amount, and approved exchange rate must be supplied together.");
            var currency = line.ForeignCurrencyCode.Trim().ToUpperInvariant();
            if (currency.Length != 3 || currency == FunctionalCurrencyCode || line.ForeignAmount <= 0m
                || DecimalScale(line.ForeignAmount.Value) > 8)
                throw new InvalidOperationException("The foreign currency measurement is invalid.");
            if (!rates.TryGetValue(line.ForeignExchangeRateId.Value, out var rate)
                || rate.Status != ExchangeRateStatus.Approved
                || rate.Purpose != ExchangeRatePurpose.Transaction
                || rate.RateDate.Date != entryDate.Date
                || rate.CurrencyCode != currency)
                throw new InvalidOperationException("The foreign exchange rate must be approved for the transaction currency and exact entry date.");
            var expectedSar = decimal.Round(line.ForeignAmount.Value * rate.SarPerUnit, 2, MidpointRounding.AwayFromZero);
            if (line.Debit + line.Credit != expectedSar)
                throw new InvalidOperationException("The journal-line amount does not equal the foreign amount translated by the approved rate.");
            result[index] = rate;
        }
        return result;
    }

    private void ValidatePersistedForeignMeasurements(JournalEntry entry)
    {
        var requests = entry.Lines.OrderBy(line => line.LineNumber).Select(line => new JournalLineRequest(
            line.LedgerAccountId, line.Debit, line.Credit, line.CostCenterId, line.Description,
            line.ForeignCurrencyCode, line.ForeignAmount, line.ForeignExchangeRateId)).ToArray();
        var rates = ValidateForeignMeasurements(entry.EntryDate, requests);
        for (var index = 0; index < requests.Length; index++)
        {
            if (rates[index]?.SarPerUnit != entry.Lines.OrderBy(line => line.LineNumber).ElementAt(index).ExchangeRateSarPerUnit)
                throw new InvalidOperationException("The stored exchange-rate snapshot does not match the approved rate.");
        }
    }

    private static bool HasAnyForeignMeasurement(JournalLineRequest line) =>
        !string.IsNullOrWhiteSpace(line.ForeignCurrencyCode)
        || line.ForeignAmount.HasValue
        || line.ForeignExchangeRateId.HasValue;

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;

    private void EnsureFunctionalCurrency(IEnumerable<JournalEntryLine> lines)
    {
        var accountIds = lines.Select(item => item.LedgerAccountId).Distinct().ToArray();
        var currencies = _context.LedgerAccounts.AsNoTracking()
            .Where(item => accountIds.Contains(item.Id)).Select(item => item.CurrencyCode).ToArray();
        if (currencies.Length != accountIds.Length || currencies.Any(currency =>
                !string.Equals(currency, FunctionalCurrencyCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Only {FunctionalCurrencyCode} accounts can be posted until foreign-currency accounting is implemented.");
    }

    private void AddAudit(long entryId, string action, string actor, string reason, string? before, string? after) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(JournalEntry),
            EntityId = entryId.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            BeforeJson = before,
            AfterJson = after,
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static string Snapshot(JournalEntry entry) => JsonSerializer.Serialize(new
    {
        entry.EntryNumber,
        entry.EntryDate,
        entry.FiscalPeriodId,
        entry.Description,
        entry.Source,
        entry.Reference,
        entry.Status,
        entry.CreatedBy,
        entry.ApprovedBy,
        entry.PostedBy,
        entry.ReversedBy,
        entry.ReversalOfJournalEntryId,
        Lines = entry.Lines.OrderBy(line => line.LineNumber)
            .Select(line => new
            {
                line.LineNumber, line.LedgerAccountId, line.Debit, line.Credit, line.CostCenterId,
                line.ForeignCurrencyCode, line.ForeignAmount, line.ForeignExchangeRateId, line.ExchangeRateSarPerUnit
            })
    });

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
