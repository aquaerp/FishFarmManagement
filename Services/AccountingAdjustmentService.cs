using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record AccountingAdjustmentRequest(
    DateTime EntryDate,
    int FiscalPeriodId,
    string Description,
    AccountingAdjustmentType Type,
    string SupportingDocumentReference,
    DateTime? ScheduledReversalDate,
    IReadOnlyList<JournalLineRequest> Lines);

public sealed record AccountingAdjustmentResult(AccountingAdjustment Adjustment, JournalEntry JournalEntry);

public sealed class AccountingAdjustmentService
{
    private const string AdjustmentSource = "AccountingAdjustment";
    private readonly FishFarmContext _context;
    private readonly GeneralLedgerService _ledger;

    public AccountingAdjustmentService(FishFarmContext context)
    {
        _context = context;
        _ledger = new GeneralLedgerService(context);
    }

    public AccountingAdjustmentResult CreateDraft(AccountingAdjustmentRequest request, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        if (string.IsNullOrWhiteSpace(request.SupportingDocumentReference))
            throw new InvalidOperationException("A supporting document reference is required for every adjustment.");
        if (request.SupportingDocumentReference.Trim().Length > 200)
            throw new InvalidOperationException("The supporting document reference cannot exceed 200 characters.");
        if (request.ScheduledReversalDate.HasValue
            && request.ScheduledReversalDate.Value.Date <= request.EntryDate.Date)
            throw new InvalidOperationException("A scheduled reversal must occur after the adjustment date.");

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var journal = _ledger.CreateDraft(new JournalDraftRequest(
            request.EntryDate,
            request.FiscalPeriodId,
            request.Description,
            AdjustmentSource,
            request.SupportingDocumentReference.Trim(),
            request.Lines), actor, reason);
        var adjustment = new AccountingAdjustment
        {
            JournalEntryId = journal.Id,
            Type = request.Type,
            SupportingDocumentReference = request.SupportingDocumentReference.Trim(),
            ScheduledReversalDate = request.ScheduledReversalDate?.Date,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim()
        };
        _context.AccountingAdjustments.Add(adjustment);
        _context.SaveChanges();
        AddAudit(adjustment, "CreateDraft", actor, reason, new { journal.Id, journal.EntryNumber });
        _context.SaveChanges();
        transaction.Commit();
        return new AccountingAdjustmentResult(adjustment, journal);
    }

    public JournalEntry ReverseDue(long adjustmentId, DateTime asOfDate, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var adjustment = _context.AccountingAdjustments.Include(item => item.JournalEntry)
            .SingleOrDefault(item => item.Id == adjustmentId)
            ?? throw new InvalidOperationException("The accounting adjustment does not exist.");
        if (!adjustment.ScheduledReversalDate.HasValue)
            throw new InvalidOperationException("This adjustment has no scheduled reversal.");
        if (adjustment.ScheduledReversalDate.Value.Date > asOfDate.Date)
            throw new InvalidOperationException("The adjustment reversal is not due yet.");
        if (adjustment.ReversalJournalEntryId.HasValue)
            throw new InvalidOperationException("This adjustment has already been reversed.");
        if (adjustment.JournalEntry.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only a posted adjustment can be reversed.");

        var reversal = _ledger.Reverse(
            adjustment.JournalEntryId,
            adjustment.ScheduledReversalDate.Value.Date,
            actor,
            reason);
        adjustment.ReversalJournalEntryId = reversal.Id;
        AddAudit(adjustment, "AutoReverse", actor, reason, new { reversal.Id, reversal.EntryNumber });
        _context.SaveChanges();
        transaction.Commit();
        return reversal;
    }

    private void AddAudit(AccountingAdjustment adjustment, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(AccountingAdjustment),
            EntityId = adjustment.Id.ToString(),
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
}
