using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record LedgerAccountBalance(
    int AccountId,
    string AccountCode,
    string AccountName,
    LedgerAccountType AccountType,
    decimal TotalDebit,
    decimal TotalCredit)
{
    public decimal NetBalance => TotalDebit - TotalCredit;
}

public sealed record GeneralLedgerTrialBalance(
    DateTime FromDate,
    DateTime ToDate,
    IReadOnlyList<LedgerAccountBalance> Accounts)
{
    public decimal TotalDebit => Accounts.Sum(account => account.TotalDebit);
    public decimal TotalCredit => Accounts.Sum(account => account.TotalCredit);
    public bool IsBalanced => TotalDebit == TotalCredit;
}

public sealed class GeneralLedgerReportingService
{
    private readonly FishFarmContext _context;

    public GeneralLedgerReportingService(FishFarmContext context) => _context = context;

    public GeneralLedgerTrialBalance GetTrialBalance(DateTime fromDate, DateTime toDate)
    {
        if (toDate.Date < fromDate.Date)
            throw new ArgumentException("The report end date must not precede its start date.", nameof(toDate));

        // Materialize before decimal aggregation because SQLite stores decimal values as exact text.
        var movements = _context.JournalEntryLines.AsNoTracking()
            .Where(line => line.JournalEntry.EntryDate >= fromDate.Date
                && line.JournalEntry.EntryDate <= toDate.Date
                && (line.JournalEntry.Status == JournalEntryStatus.Posted
                    || line.JournalEntry.Status == JournalEntryStatus.Reversed))
            .Select(line => new
            {
                line.LedgerAccountId,
                line.LedgerAccount.Code,
                line.LedgerAccount.NameAr,
                line.LedgerAccount.Type,
                line.Debit,
                line.Credit
            })
            .AsEnumerable()
            .GroupBy(line => new { line.LedgerAccountId, line.Code, line.NameAr, line.Type })
            .Select(group => new LedgerAccountBalance(
                group.Key.LedgerAccountId,
                group.Key.Code,
                group.Key.NameAr,
                group.Key.Type,
                group.Sum(line => line.Debit),
                group.Sum(line => line.Credit)))
            .OrderBy(account => account.AccountCode)
            .ToArray();

        return new GeneralLedgerTrialBalance(fromDate.Date, toDate.Date, movements);
    }
}
