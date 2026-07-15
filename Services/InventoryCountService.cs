using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record InventoryCountDraftRequest(
    InventoryCountType CountType,
    DateTime CountDate,
    string Reference,
    IReadOnlyCollection<int>? InventoryItemIds = null,
    string? Notes = null);

public sealed record InventoryCountApprovalResult(
    InventoryCount Count,
    IReadOnlyList<StockMovement> Movements,
    IReadOnlyList<JournalEntry> JournalEntries);

public sealed class InventoryCountService
{
    private readonly FishFarmContext _context;

    public InventoryCountService(FishFarmContext context) => _context = context;

    public InventoryCount CreateDraft(InventoryCountDraftRequest request, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        if (string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Trim().Length > 100)
            throw new InvalidOperationException("A count reference of at most 100 characters is required.");

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var query = _context.InventoryItems.AsNoTracking().Where(item => item.IsActive);
        if (request.CountType == InventoryCountType.Periodic)
        {
            var ids = request.InventoryItemIds?.Distinct().ToArray() ?? [];
            if (ids.Length == 0) throw new InvalidOperationException("A periodic count must include at least one inventory item.");
            query = query.Where(item => ids.Contains(item.Id));
            if (query.Count() != ids.Length)
                throw new InvalidOperationException("Every periodic-count item must exist and be active.");
        }

        var items = query.OrderBy(item => item.Id).ToArray();
        if (items.Length == 0) throw new InvalidOperationException("The count scope has no active inventory items.");
        if (items.Any(item => item.CurrentStock < 0m || item.UnitCost < 0m))
            throw new InvalidOperationException("Invalid inventory balances must be investigated before starting a count.");

        var count = new InventoryCount
        {
            CountNumber = $"CNT-{request.CountDate:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
            CountType = request.CountType,
            CountDate = request.CountDate.Date,
            Reference = request.Reference.Trim(),
            Notes = NullIfWhiteSpace(request.Notes),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            Lines = items.Select(item => new InventoryCountLine
            {
                InventoryItemId = item.Id,
                BookQuantitySnapshot = item.CurrentStock,
                UnitCostSnapshot = item.UnitCost
            }).ToList()
        };
        _context.InventoryCounts.Add(count);
        _context.SaveChanges();
        AddAudit(count, "CreateDraft", actor, reason, new { request.CountType, ScopeCount = items.Length, request.Reference });
        _context.SaveChanges();
        transaction.Commit();
        return count;
    }

    public InventoryCountLine RecordActualQuantity(long countId, int inventoryItemId, decimal actualQuantity,
        string? varianceReason, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        if (actualQuantity < 0m || decimal.Round(actualQuantity, 3) != actualQuantity)
            throw new InvalidOperationException("Actual quantity must be non-negative with no more than three decimal places.");
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var count = Load(countId);
        EnsureDraftOwner(count, actor);
        var line = count.Lines.SingleOrDefault(value => value.InventoryItemId == inventoryItemId)
            ?? throw new InvalidOperationException("The inventory item is outside the count scope.");
        var variance = actualQuantity - line.BookQuantitySnapshot;
        if (variance != 0m && string.IsNullOrWhiteSpace(varianceReason))
            throw new InvalidOperationException("Every inventory variance requires a reason.");
        line.ActualQuantity = actualQuantity;
        line.VarianceQuantity = variance;
        line.VarianceValue = decimal.Round(variance * line.UnitCostSnapshot, 2, MidpointRounding.AwayFromZero);
        line.VarianceReason = variance == 0m ? null : varianceReason!.Trim();
        AddAudit(count, "RecordActualQuantity", actor, reason,
            new { line.InventoryItemId, line.BookQuantitySnapshot, line.ActualQuantity, line.VarianceQuantity, line.VarianceValue, line.VarianceReason });
        _context.SaveChanges();
        transaction.Commit();
        return line;
    }

    public InventoryCount Submit(long countId, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        var count = Load(countId);
        EnsureDraftOwner(count, actor);
        if (count.Lines.Any(line => !line.ActualQuantity.HasValue))
            throw new InvalidOperationException("All scoped inventory items must be counted before submission.");
        if (count.Lines.Any(line => line.VarianceQuantity != 0m && string.IsNullOrWhiteSpace(line.VarianceReason)))
            throw new InvalidOperationException("Every inventory variance requires a reason before submission.");
        count.Status = InventoryCountStatus.Submitted;
        count.SubmittedAtUtc = DateTime.UtcNow;
        AddAudit(count, "Submit", actor, reason, new { Lines = count.Lines.Count, Variances = count.Lines.Count(line => line.VarianceQuantity != 0m) });
        _context.SaveChanges();
        return count;
    }

    public InventoryCountApprovalResult Approve(long countId, int fiscalPeriodId, string approver, string reason)
    {
        RequireActorAndReason(approver, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
        var count = Load(countId);
        if (count.Status != InventoryCountStatus.Submitted)
            throw new InvalidOperationException("Only a submitted inventory count can be approved.");
        if (string.Equals(count.CreatedBy, approver.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The count creator cannot approve the same count.");

        var itemIds = count.Lines.Select(line => line.InventoryItemId).ToArray();
        var current = _context.InventoryItems.AsNoTracking().Where(item => itemIds.Contains(item.Id)).ToDictionary(item => item.Id);
        foreach (var line in count.Lines)
        {
            if (!current.TryGetValue(line.InventoryItemId, out var item)
                || item.CurrentStock != line.BookQuantitySnapshot || item.UnitCost != line.UnitCostSnapshot)
                throw new InvalidOperationException("Inventory changed after the count started; cancel this approval and perform a fresh count.");
        }

        var movementService = new InventoryTransactionService(_context);
        var postingService = new OperationalPostingService(_context);
        var ledgerService = new GeneralLedgerService(_context);
        var movements = new List<StockMovement>();
        var journals = new List<JournalEntry>();
        foreach (var line in count.Lines.Where(value => value.VarianceQuantity != 0m).OrderBy(value => value.Id))
        {
            if (line.VarianceQuantity > 0m && line.UnitCostSnapshot <= 0m)
                throw new InvalidOperationException("A positive count variance requires an established positive weighted-average cost.");
            var movementType = line.VarianceQuantity > 0m
                ? StockMovementType.AdjustmentIncrease
                : StockMovementType.AdjustmentDecrease;
            var quantity = Math.Abs(line.VarianceQuantity);
            var movement = movementService.CreateDraft(new InventoryMovementDraftRequest(
                line.InventoryItemId, movementType, quantity,
                line.VarianceQuantity > 0m ? line.UnitCostSnapshot : null,
                count.CountDate, $"{count.CountNumber}-{line.InventoryItemId}",
                $"Inventory count {count.Reference}: {line.VarianceReason}"),
                count.CreatedBy, $"Generated from submitted inventory count {count.CountNumber}");
            movementService.Approve(movement.Id, approver, reason);
            var journal = postingService.CreateInventoryMovementDraft(
                movement.Id, fiscalPeriodId, count.CreatedBy, $"Automatic inventory-count variance {count.CountNumber}");
            ledgerService.Approve(journal.Id, approver, reason);
            ledgerService.Post(journal.Id, approver, reason);
            line.StockMovementId = movement.Id;
            line.JournalEntryId = journal.Id;
            movements.Add(movement);
            journals.Add(journal);
        }

        count.Status = InventoryCountStatus.Approved;
        count.ApprovedAtUtc = DateTime.UtcNow;
        count.ApprovedBy = approver.Trim();
        count.ApprovalReason = reason.Trim();
        AddAudit(count, "ApproveApplyAndPost", approver, reason,
            new { Movements = movements.Select(value => value.Id), Journals = journals.Select(value => value.Id) });
        _context.SaveChanges();
        transaction.Commit();
        return new InventoryCountApprovalResult(count, movements, journals);
        }
        catch
        {
            transaction.Rollback();
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public InventoryCount Reject(long countId, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        var count = Load(countId);
        if (count.Status != InventoryCountStatus.Submitted)
            throw new InvalidOperationException("Only a submitted inventory count can be rejected.");
        if (string.Equals(count.CreatedBy, actor.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The count creator cannot reject the same count.");
        count.Status = InventoryCountStatus.Rejected;
        count.RejectedAtUtc = DateTime.UtcNow;
        count.RejectedBy = actor.Trim();
        count.RejectionReason = reason.Trim();
        AddAudit(count, "Reject", actor, reason, new { count.CountNumber });
        _context.SaveChanges();
        return count;
    }

    private InventoryCount Load(long countId) => _context.InventoryCounts
        .Include(value => value.Lines)
        .SingleOrDefault(value => value.Id == countId)
        ?? throw new InvalidOperationException("The inventory count does not exist.");

    private static void EnsureDraftOwner(InventoryCount count, string actor)
    {
        if (count.Status != InventoryCountStatus.Draft)
            throw new InvalidOperationException("Only a draft inventory count can be changed.");
        if (!string.Equals(count.CreatedBy, actor.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only the count creator can record or submit its results.");
    }

    private void AddAudit(InventoryCount count, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(InventoryCount),
            EntityId = count.Id.ToString(),
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

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
