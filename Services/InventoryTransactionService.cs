using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record InventoryMovementDraftRequest(
    int InventoryItemId,
    StockMovementType MovementType,
    decimal Quantity,
    decimal? ReceiptUnitCost,
    DateTime MovementDate,
    string Reference,
    string? Notes = null);

public sealed record InventoryMovementApprovalResult(
    StockMovement Movement,
    decimal QuantityBefore,
    decimal QuantityAfter,
    decimal WeightedAverageUnitCost,
    decimal MovementValue);

public sealed class InventoryTransactionService
{
    public const ValuationMethod CostingMethod = ValuationMethod.WeightedAverage;
    private readonly FishFarmContext _context;

    public InventoryTransactionService(FishFarmContext context) => _context = context;

    public StockMovement CreateDraft(InventoryMovementDraftRequest request, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        if (request.Quantity <= 0m || decimal.Round(request.Quantity, 3) != request.Quantity)
            throw new InvalidOperationException("Inventory quantity must be positive with no more than three decimal places.");
        if (string.IsNullOrWhiteSpace(request.Reference))
            throw new InvalidOperationException("An inventory movement reference is required.");
        if (request.Reference.Trim().Length > 100)
            throw new InvalidOperationException("The inventory movement reference cannot exceed 100 characters.");
        var inbound = IsInbound(request.MovementType);
        if (!inbound && !IsOutbound(request.MovementType))
            throw new InvalidOperationException("This movement type requires a dedicated inventory workflow.");
        if (inbound && (!request.ReceiptUnitCost.HasValue || request.ReceiptUnitCost <= 0m
                || decimal.Round(request.ReceiptUnitCost.Value, 2) != request.ReceiptUnitCost.Value))
            throw new InvalidOperationException("Inbound inventory requires a positive SAR unit cost with two-decimal precision.");
        if (!inbound && request.ReceiptUnitCost.HasValue)
            throw new InvalidOperationException("Outbound inventory is costed by the approved weighted-average cost, not a manual cost.");
        if (!_context.InventoryItems.AsNoTracking().Any(item => item.Id == request.InventoryItemId && item.IsActive))
            throw new InvalidOperationException("The active inventory item does not exist.");

        var unitCost = request.ReceiptUnitCost ?? 0m;
        var movement = new StockMovement
        {
            InventoryItemId = request.InventoryItemId,
            MovementType = request.MovementType,
            MovementDate = request.MovementDate,
            Quantity = request.Quantity,
            UnitCost = unitCost,
            TotalCost = decimal.Round(request.Quantity * unitCost, 2, MidpointRounding.AwayFromZero),
            Reference = request.Reference.Trim(),
            ReferenceNumber = request.Reference.Trim(),
            Notes = request.Notes?.Trim(),
            IsApproved = false,
            CreatedBy = actor.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);
        _context.SaveChanges();
        AddAudit(movement, "CreateDraft", actor, reason, new { request.MovementType, request.Quantity, request.ReceiptUnitCost });
        _context.SaveChanges();
        transaction?.Commit();
        return movement;
    }

    public InventoryMovementApprovalResult Approve(long movementId, string approver, string reason)
    {
        RequireActorAndReason(approver, reason);
        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var movement = _context.StockMovements.Include(value => value.InventoryItem)
            .SingleOrDefault(value => value.Id == movementId)
            ?? throw new InvalidOperationException("The inventory movement does not exist.");
        if (movement.IsApproved)
            throw new InvalidOperationException("The inventory movement is already approved.");
        if (movement.IsRejected || movement.IsCancelled)
            throw new InvalidOperationException("A rejected or cancelled movement cannot be approved.");
        if (string.Equals(movement.CreatedBy, approver.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The movement creator cannot approve the same movement.");

        var item = movement.InventoryItem;
        var before = item.CurrentStock;
        if (before < 0m || item.UnitCost < 0m)
            throw new InvalidOperationException("The inventory master balance or cost is invalid and must be investigated before approval.");
        var inbound = IsInbound(movement.MovementType);
        decimal after;
        decimal movementValue;
        if (inbound)
        {
            after = before + movement.Quantity;
            movementValue = movement.TotalCost;
            var existingValue = decimal.Round(before * item.UnitCost, 2, MidpointRounding.AwayFromZero);
            item.UnitCost = decimal.Round((existingValue + movementValue) / after, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            if (movement.Quantity > before)
                throw new InvalidOperationException("The issue would create negative inventory and is not permitted.");
            after = before - movement.Quantity;
            movement.UnitCost = item.UnitCost;
            movementValue = decimal.Round(movement.Quantity * item.UnitCost, 2, MidpointRounding.AwayFromZero);
            movement.TotalCost = movementValue;
        }

        item.CurrentStock = after;
        item.LastModifiedDate = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = approver.Trim();
        movement.BalanceBefore = before;
        movement.BalanceAfter = after;
        movement.IsApproved = true;
        movement.ApprovedDate = DateTime.UtcNow;
        movement.ApprovedAt = DateTime.UtcNow;
        movement.ApprovedByUsername = approver.Trim();
        movement.ApprovalReason = reason.Trim();
        _context.InventoryValuations.Add(new InventoryValuation
        {
            InventoryItemId = item.Id,
            ValuationDate = movement.MovementDate,
            Method = CostingMethod,
            Quantity = after,
            UnitCost = item.UnitCost,
            TotalValue = decimal.Round(after * item.UnitCost, 2, MidpointRounding.AwayFromZero),
            IsApproved = true,
            ApprovedDate = DateTime.UtcNow,
            CreatedBy = approver.Trim(),
            CreatedDate = DateTime.UtcNow,
            Notes = $"Approved movement {movement.Reference}"
        });
        AddAudit(movement, "ApproveAndApply", approver, reason,
            new { QuantityBefore = before, QuantityAfter = after, item.UnitCost, MovementValue = movementValue, Method = CostingMethod });
        _context.SaveChanges();
        transaction?.Commit();
        return new InventoryMovementApprovalResult(movement, before, after, item.UnitCost, movementValue);
    }

    private void AddAudit(StockMovement movement, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(StockMovement),
            EntityId = movement.Id.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(details),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static bool IsInbound(StockMovementType type) => type is
        StockMovementType.Purchase or StockMovementType.Found or StockMovementType.AdjustmentIncrease;

    private static bool IsOutbound(StockMovementType type) => type is
        StockMovementType.Sale or StockMovementType.Consumption or StockMovementType.Damage
        or StockMovementType.Expiry or StockMovementType.Expired or StockMovementType.Loss
        or StockMovementType.Waste or StockMovementType.AdjustmentDecrease;

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }
}
