using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record OpeningBalanceRequest(int LedgerAccountId, decimal Debit, decimal Credit, string? Description = null);
public sealed record FiscalYearOpeningResult(FiscalYear FiscalYear, JournalEntry OpeningBalanceDraft);

public sealed class FiscalYearClosingService
{
    private const string OpeningSource = "OpeningBalance";
    private const string ClosingSource = "YearEndClosing";
    private readonly FishFarmContext _context;
    private readonly GeneralLedgerService _ledger;

    public FiscalYearClosingService(FishFarmContext context)
    {
        _context = context;
        _ledger = new GeneralLedgerService(context);
    }

    public JournalEntry CreateOpeningBalanceDraft(
        int fiscalYearId,
        IReadOnlyList<OpeningBalanceRequest> balances,
        string actor,
        string reason)
    {
        RequireActorAndReason(actor, reason);
        if (balances.Count < 2)
            throw new InvalidOperationException("Opening balances require at least two account balances.");

        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var year = LoadYear(fiscalYearId);
        EnsureYearIsOpen(year);
        if (year.OpeningBalanceJournalEntryId.HasValue)
            throw new InvalidOperationException("An opening balance journal already exists for this fiscal year.");

        var accountIds = balances.Select(item => item.LedgerAccountId).Distinct().ToArray();
        if (accountIds.Length != balances.Count)
            throw new InvalidOperationException("Each opening balance account can appear only once.");
        var accounts = _context.LedgerAccounts.AsNoTracking()
            .Where(item => accountIds.Contains(item.Id)).ToDictionary(item => item.Id);
        if (accounts.Count != accountIds.Length || accounts.Values.Any(item =>
                !item.IsActive || !item.AllowsPosting || item.CurrencyCode != "SAR"
                || item.Type is LedgerAccountType.Revenue or LedgerAccountType.Expense))
            throw new InvalidOperationException("Opening balances may use only active SAR balance-sheet posting accounts.");

        var period = FindPeriod(year, year.StartDate);
        var entry = _ledger.CreateDraft(new JournalDraftRequest(
            year.StartDate,
            period.Id,
            $"Opening balances for {year.Name}",
            OpeningSource,
            $"OPEN-{year.Name}",
            balances.Select(item => new JournalLineRequest(
                item.LedgerAccountId, item.Debit, item.Credit, Description: item.Description)).ToArray()),
            actor,
            reason);

        year.OpeningBalanceJournalEntryId = entry.Id;
        AddYearAudit(year, "CreateOpeningBalanceDraft", actor, reason, new { entry.Id, entry.EntryNumber });
        _context.SaveChanges();
        transaction?.Commit();
        return entry;
    }

    public JournalEntry CreateYearEndClosingDraft(
        int fiscalYearId,
        int retainedEarningsAccountId,
        string actor,
        string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var year = LoadYear(fiscalYearId);
        EnsureYearIsOpen(year);
        if (year.ClosingJournalEntryId.HasValue)
            throw new InvalidOperationException("A year-end closing journal already exists for this fiscal year.");

        EnsureCompletePeriodCoverage(year);
        var closingPeriod = FindPeriod(year, year.EndDate);
        if (year.Periods.Any(item => item.Id != closingPeriod.Id && item.Status != FiscalPeriodStatus.Closed))
            throw new InvalidOperationException("All periods before the final fiscal period must be closed.");
        if (_context.JournalEntries.Any(item => item.FiscalPeriod.FiscalYearId == year.Id
                && (item.Status == JournalEntryStatus.Draft || item.Status == JournalEntryStatus.Approved)))
            throw new InvalidOperationException("All existing fiscal-year journals must be posted before closing.");

        var retainedEarnings = _context.LedgerAccounts.AsNoTracking()
            .SingleOrDefault(item => item.Id == retainedEarningsAccountId)
            ?? throw new InvalidOperationException("The retained earnings account does not exist.");
        if (!retainedEarnings.IsActive || !retainedEarnings.AllowsPosting
            || retainedEarnings.Type != LedgerAccountType.Equity || retainedEarnings.CurrencyCode != "SAR")
            throw new InvalidOperationException("Retained earnings must be an active SAR equity posting account.");

        var incomeBalances = GetAccountBalances(year, includeIncomeStatement: true)
            .Where(item => item.Type is LedgerAccountType.Revenue or LedgerAccountType.Expense)
            .Where(item => item.Net != 0m)
            .ToArray();
        if (incomeBalances.Length == 0)
            throw new InvalidOperationException("The fiscal year has no income-statement balances to close.");

        var lines = incomeBalances.Select(item => item.Net > 0m
                ? new JournalLineRequest(item.AccountId, 0m, item.Net, Description: "Close debit balance")
                : new JournalLineRequest(item.AccountId, -item.Net, 0m, Description: "Close credit balance"))
            .ToList();
        var debit = lines.Sum(item => item.Debit);
        var credit = lines.Sum(item => item.Credit);
        if (debit > credit)
            lines.Add(new JournalLineRequest(retainedEarnings.Id, 0m, debit - credit, Description: "Transfer profit to retained earnings"));
        else if (credit > debit)
            lines.Add(new JournalLineRequest(retainedEarnings.Id, credit - debit, 0m, Description: "Transfer loss to retained earnings"));

        var entry = _ledger.CreateDraft(new JournalDraftRequest(
            year.EndDate,
            closingPeriod.Id,
            $"Year-end income statement closing for {year.Name}",
            ClosingSource,
            $"CLOSE-{year.Name}",
            lines), actor, reason);
        year.ClosingJournalEntryId = entry.Id;
        AddYearAudit(year, "CreateYearEndClosingDraft", actor, reason, new { entry.Id, entry.EntryNumber });
        _context.SaveChanges();
        transaction.Commit();
        return entry;
    }

    public void CloseFiscalYear(int fiscalYearId, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var year = LoadYear(fiscalYearId);
        if (year.IsClosed) return;
        if (!year.ClosingJournalEntryId.HasValue)
            throw new InvalidOperationException("A posted year-end closing journal is required.");
        var closingEntry = _context.JournalEntries.AsNoTracking()
            .SingleOrDefault(item => item.Id == year.ClosingJournalEntryId.Value);
        if (closingEntry?.Status != JournalEntryStatus.Posted || closingEntry.Source != ClosingSource)
            throw new InvalidOperationException("The year-end closing journal must be posted before closing the fiscal year.");
        if (_context.JournalEntries.Any(item => item.FiscalPeriod.FiscalYearId == year.Id
                && (item.Status == JournalEntryStatus.Draft || item.Status == JournalEntryStatus.Approved)))
            throw new InvalidOperationException("A fiscal year with unposted journals cannot be closed.");

        var now = DateTime.UtcNow;
        foreach (var period in year.Periods)
        {
            period.Status = FiscalPeriodStatus.Closed;
            period.ClosedAtUtc ??= now;
            period.ClosedBy ??= actor.Trim();
        }
        year.IsClosed = true;
        year.ClosedAtUtc = now;
        year.ClosedBy = actor.Trim();
        AddYearAudit(year, "Close", actor, reason, new { year.ClosingJournalEntryId });
        _context.SaveChanges();
        transaction.Commit();
    }

    public FiscalYearOpeningResult CreateNextFiscalYearWithOpeningDraft(
        int closedFiscalYearId,
        string nextYearName,
        DateTime nextYearStart,
        DateTime nextYearEnd,
        string actor,
        string reason)
    {
        RequireActorAndReason(actor, reason);
        if (string.IsNullOrWhiteSpace(nextYearName))
            throw new ArgumentException("Fiscal year name is required.", nameof(nextYearName));
        if (nextYearEnd.Date < nextYearStart.Date)
            throw new InvalidOperationException("Fiscal year end must not precede its start.");
        if (nextYearStart.Day != 1 || nextYearEnd.Date != nextYearStart.Date.AddYears(1).AddDays(-1))
            throw new InvalidOperationException("The next fiscal year must span twelve complete months.");

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var closedYear = LoadYear(closedFiscalYearId);
        if (!closedYear.IsClosed)
            throw new InvalidOperationException("The preceding fiscal year must be closed first.");
        if (nextYearStart.Date != closedYear.EndDate.Date.AddDays(1))
            throw new InvalidOperationException("The next fiscal year must start on the day after the closed year ends.");
        if (_context.FiscalYears.Any(item => item.Name == nextYearName.Trim()
                || (item.StartDate <= nextYearEnd.Date && item.EndDate >= nextYearStart.Date)))
            throw new InvalidOperationException("The next fiscal year name or date range overlaps an existing year.");

        var carryForward = GetAccountBalances(closedYear, includeIncomeStatement: false)
            .Where(item => item.Type is LedgerAccountType.Asset or LedgerAccountType.Liability or LedgerAccountType.Equity)
            .Where(item => item.Net != 0m)
            .Select(item => item.Net > 0m
                ? new OpeningBalanceRequest(item.AccountId, item.Net, 0m, "Balance carried forward")
                : new OpeningBalanceRequest(item.AccountId, 0m, -item.Net, "Balance carried forward"))
            .ToArray();
        if (carryForward.Length < 2)
            throw new InvalidOperationException("There are no balanced statement-of-financial-position balances to carry forward.");

        var nextYear = new FiscalYear
        {
            Name = nextYearName.Trim(),
            StartDate = nextYearStart.Date,
            EndDate = nextYearEnd.Date,
            Periods = CreateMonthlyPeriods(nextYearStart.Date, nextYearEnd.Date)
        };
        _context.FiscalYears.Add(nextYear);
        _context.SaveChanges();

        var opening = CreateOpeningBalanceDraft(nextYear.Id, carryForward, actor, reason);
        AddYearAudit(nextYear, "CreateFromClosedYear", actor, reason, new { PreviousFiscalYearId = closedYear.Id, opening.Id });
        _context.SaveChanges();
        transaction.Commit();
        return new FiscalYearOpeningResult(nextYear, opening);
    }

    private FiscalYear LoadYear(int fiscalYearId) =>
        _context.FiscalYears.Include(item => item.Periods)
            .SingleOrDefault(item => item.Id == fiscalYearId)
        ?? throw new InvalidOperationException("The fiscal year does not exist.");

    private static void EnsureYearIsOpen(FiscalYear year)
    {
        if (year.IsClosed)
            throw new InvalidOperationException("The fiscal year is closed.");
        if (year.EndDate.Date < year.StartDate.Date)
            throw new InvalidOperationException("The fiscal year date range is invalid.");
    }

    private static FiscalPeriod FindPeriod(FiscalYear year, DateTime date)
    {
        var period = year.Periods.SingleOrDefault(item => item.StartDate.Date <= date.Date && item.EndDate.Date >= date.Date)
            ?? throw new InvalidOperationException("No fiscal period covers the required journal date.");
        if (period.Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException("The required fiscal period is closed.");
        return period;
    }

    private static void EnsureCompletePeriodCoverage(FiscalYear year)
    {
        var expectedStart = year.StartDate.Date;
        foreach (var period in year.Periods.OrderBy(item => item.StartDate))
        {
            if (period.StartDate.Date != expectedStart || period.EndDate.Date < period.StartDate.Date
                || period.EndDate.Date > year.EndDate.Date)
                throw new InvalidOperationException("Fiscal periods must cover the fiscal year continuously without gaps or overlaps.");
            expectedStart = period.EndDate.Date.AddDays(1);
        }
        if (expectedStart != year.EndDate.Date.AddDays(1))
            throw new InvalidOperationException("Fiscal periods must cover the fiscal year continuously without gaps or overlaps.");
    }

    private AccountBalance[] GetAccountBalances(FiscalYear year, bool includeIncomeStatement) =>
        _context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntry.FiscalPeriod.FiscalYearId == year.Id
                && (line.JournalEntry.Status == JournalEntryStatus.Posted
                    || line.JournalEntry.Status == JournalEntryStatus.Reversed)
                && (includeIncomeStatement
                    || (line.LedgerAccount.Type != LedgerAccountType.Revenue
                        && line.LedgerAccount.Type != LedgerAccountType.Expense)))
            .Select(line => new { line.LedgerAccountId, line.LedgerAccount.Type, line.Debit, line.Credit })
            .AsEnumerable()
            .GroupBy(line => new { line.LedgerAccountId, line.Type })
            .Select(group => new AccountBalance(
                group.Key.LedgerAccountId,
                group.Key.Type,
                group.Sum(line => line.Debit) - group.Sum(line => line.Credit)))
            .ToArray();

    private static List<FiscalPeriod> CreateMonthlyPeriods(DateTime start, DateTime end)
    {
        var periods = new List<FiscalPeriod>();
        var cursor = start;
        while (cursor <= end)
        {
            var periodEnd = cursor.AddMonths(1).AddDays(-1);
            if (periodEnd > end) periodEnd = end;
            periods.Add(new FiscalPeriod
            {
                Name = $"{cursor:yyyy-MM}",
                StartDate = cursor,
                EndDate = periodEnd,
                Status = FiscalPeriodStatus.Open
            });
            cursor = periodEnd.AddDays(1);
        }
        return periods;
    }

    private void AddYearAudit(FiscalYear year, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(FiscalYear),
            EntityId = year.Id.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(details),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }

    private sealed record AccountBalance(int AccountId, LedgerAccountType Type, decimal Net);
}
