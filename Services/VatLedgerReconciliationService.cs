using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed class VatLedgerReconciliationService
{
    private static readonly PostingEventType[] OutputEvents =
    [
        PostingEventType.SalesCompleted,
        PostingEventType.TaxCreditNoteIssued,
        PostingEventType.TaxDebitNoteIssued
    ];

    private readonly FishFarmContext _context;

    public VatLedgerReconciliationService(FishFarmContext context) => _context = context;

    public VatReturnLedgerReconciliation Reconcile(int vatReturnId, string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
        var vatReturn = _context.VATReturns.AsNoTracking().SingleOrDefault(item => item.Id == vatReturnId)
            ?? throw new InvalidOperationException("The VAT return does not exist.");
        if (vatReturn.Status is not (VATReturnStatus.Approved or VATReturnStatus.Submitted))
            throw new InvalidOperationException("Only an approved or submitted VAT return can be reconciled.");
        if (vatReturn.PeriodStartDate.Date > vatReturn.PeriodEndDate.Date)
            throw new InvalidOperationException("The VAT return period is invalid.");
        var version = (_context.VatReturnLedgerReconciliations
            .Where(item => item.VatReturnId == vatReturn.Id)
            .Select(item => (int?)item.Version).Max() ?? 0) + 1;

        var configuration = _context.AccountingConfigurations.AsNoTracking()
            .Where(item => item.Status == AccountingConfigurationStatus.Approved)
            .OrderByDescending(item => item.Version)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No approved accounting configuration is available.");
        var outputAccountIds = _context.PostingMappings.AsNoTracking()
            .Where(item => item.AccountingConfigurationId == configuration.Id && item.IsActive
                && OutputEvents.Contains(item.EventType) && item.Component == PostingComponent.OutputVat)
            .Select(item => item.LedgerAccountId).Distinct().ToArray();
        var inputAccountIds = _context.PostingMappings.AsNoTracking()
            .Where(item => item.AccountingConfigurationId == configuration.Id && item.IsActive
                && item.EventType == PostingEventType.PurchaseReceived && item.Component == PostingComponent.InputVat)
            .Select(item => item.LedgerAccountId).Distinct().ToArray();
        if (outputAccountIds.Length != 1 || inputAccountIds.Length != 1)
            throw new InvalidOperationException("The approved VAT posting map must resolve to one output and one input VAT account.");

        var records = _context.OperationalPostingRecords.AsNoTracking()
            .Include(item => item.JournalEntry).ThenInclude(item => item.Lines)
            .Where(item => item.JournalEntry.EntryDate.Date >= vatReturn.PeriodStartDate.Date
                && item.JournalEntry.EntryDate.Date <= vatReturn.PeriodEndDate.Date
                && item.JournalEntry.Status == JournalEntryStatus.Posted
                && (OutputEvents.Contains(item.EventType) || item.EventType == PostingEventType.PurchaseReceived))
            .ToArray();
        var outputRecords = records.Where(item => OutputEvents.Contains(item.EventType)).ToArray();
        var purchaseRecords = records.Where(item => item.EventType == PostingEventType.PurchaseReceived).ToArray();
        var ledgerOutput = decimal.Round(outputRecords.SelectMany(item => item.JournalEntry.Lines)
            .Where(line => line.LedgerAccountId == outputAccountIds[0])
            .Sum(line => line.Credit - line.Debit), 2);
        var ledgerInput = decimal.Round(purchaseRecords.SelectMany(item => item.JournalEntry.Lines)
            .Where(line => line.LedgerAccountId == inputAccountIds[0])
            .Sum(line => line.Debit - line.Credit), 2);
        var outputDifference = decimal.Round(vatReturn.Box6_VATOnSales - ledgerOutput, 2);
        var inputDifference = decimal.Round(vatReturn.Box10_VATOnPurchases - ledgerInput, 2);
        var evidence = new VatReturnLedgerReconciliation
        {
            VatReturnId = vatReturn.Id,
            Version = version,
            PeriodStartDate = vatReturn.PeriodStartDate.Date,
            PeriodEndDate = vatReturn.PeriodEndDate.Date,
            DeclaredOutputVat = vatReturn.Box6_VATOnSales,
            DeclaredInputVat = vatReturn.Box10_VATOnPurchases,
            LedgerOutputVat = ledgerOutput,
            LedgerInputVat = ledgerInput,
            OutputDifference = outputDifference,
            InputDifference = inputDifference,
            SalesPostingCount = outputRecords.Count(item => item.EventType == PostingEventType.SalesCompleted),
            PurchasePostingCount = purchaseRecords.Length,
            CreditNotePostingCount = outputRecords.Count(item => item.EventType == PostingEventType.TaxCreditNoteIssued),
            DebitNotePostingCount = outputRecords.Count(item => item.EventType == PostingEventType.TaxDebitNoteIssued),
            IsReconciled = outputDifference == 0m && inputDifference == 0m,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            Reason = reason.Trim()
        };
        _context.VatReturnLedgerReconciliations.Add(evidence);
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(VatReturnLedgerReconciliation),
            EntityId = vatReturn.Id.ToString(),
            Action = evidence.IsReconciled ? "Reconcile" : "ReconciliationMismatch",
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                evidence.DeclaredOutputVat,
                evidence.Version,
                evidence.DeclaredInputVat,
                evidence.LedgerOutputVat,
                evidence.LedgerInputVat,
                evidence.OutputDifference,
                evidence.InputDifference,
                evidence.IsReconciled
            }),
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        _context.SaveChanges();
        return evidence;
    }
}
