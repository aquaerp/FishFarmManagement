using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record PurchaseReceivingInventoryResult(
    PurchaseReceiving Receiving,
    IReadOnlyList<InventoryMovementApprovalResult> InventoryMovements);

public sealed class PurchaseReceivingInventoryService
{
    private readonly FishFarmContext _context;

    public PurchaseReceivingInventoryService(FishFarmContext context) => _context = context;

    public PurchaseReceivingInventoryResult ApproveAndAddToInventory(
        int purchaseReceivingId,
        string approver,
        string reason)
    {
        RequireActorAndReason(approver, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var receiving = _context.PurchaseReceivings
            .Include(value => value.Items)
            .Include(value => value.PurchaseOrder).ThenInclude(value => value.Items)
            .SingleOrDefault(value => value.Id == purchaseReceivingId)
            ?? throw new InvalidOperationException("The purchase receiving record does not exist.");
        if (receiving.IsApproved)
            throw new InvalidOperationException("The purchase receiving record is already approved.");
        var creator = receiving.CreatedBy ?? receiving.ReceivedBy;
        if (string.IsNullOrWhiteSpace(creator))
            throw new InvalidOperationException("The purchase receiving creator is required.");
        if (string.Equals(creator.Trim(), approver.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The purchase receiving creator cannot approve the same receipt.");
        if (!receiving.IsFullReceiving || !receiving.QualityInspectionCompleted
            || receiving.OverallQualityResult != QualityTestResult.Passed)
            throw new InvalidOperationException("Only a complete receiving that passed quality inspection can be approved.");
        if (receiving.Items.Count == 0)
            throw new InvalidOperationException("The purchase receiving record has no items.");

        var orderItems = receiving.PurchaseOrder.Items.ToDictionary(value => value.Id);
        var inventoryItemIds = receiving.Items.Select(value => value.InventoryItemId).Distinct().ToArray();
        var activeInventoryIds = _context.InventoryItems.AsNoTracking()
            .Where(value => inventoryItemIds.Contains(value.Id) && value.IsActive)
            .Select(value => value.Id).ToHashSet();
        foreach (var item in receiving.Items)
        {
            var accepted = item.AcceptedQuantity;
            if (item.AddedToStock || item.StockMovementId.HasValue)
                throw new InvalidOperationException($"Receiving item {item.Id} has already been added to inventory.");
            if (accepted <= 0m || item.RejectedQuantity < 0m || item.RejectedQuantity > item.ReceivedQuantity)
                throw new InvalidOperationException($"Receiving item {item.Id} has an invalid accepted quantity.");
            if (!item.QualityAccepted || item.QualityResult == QualityTestResult.Failed)
                throw new InvalidOperationException($"Receiving item {item.Id} did not pass item-level quality inspection.");
            if (item.UnitPrice <= 0m || decimal.Round(item.UnitPrice, 2) != item.UnitPrice)
                throw new InvalidOperationException($"Receiving item {item.Id} requires a valid two-decimal unit price.");
            if (!activeInventoryIds.Contains(item.InventoryItemId))
                throw new InvalidOperationException($"Receiving item {item.Id} does not reference an active inventory item.");
            if (!orderItems.TryGetValue(item.PurchaseOrderItemId, out var orderItem)
                || orderItem.InventoryItemId != item.InventoryItemId)
                throw new InvalidOperationException($"Receiving item {item.Id} does not match its purchase-order item.");
            if (orderItem.ReceivedQuantity + accepted > orderItem.Quantity)
                throw new InvalidOperationException($"Receiving item {item.Id} exceeds the ordered quantity.");
        }

        var inventory = new InventoryTransactionService(_context);
        var results = new List<InventoryMovementApprovalResult>(receiving.Items.Count);
        foreach (var item in receiving.Items.OrderBy(value => value.Id))
        {
            var accepted = item.AcceptedQuantity;
            var movement = inventory.CreateDraft(new InventoryMovementDraftRequest(
                    item.InventoryItemId, StockMovementType.Purchase, accepted, item.UnitPrice,
                    receiving.ReceivingDate, $"{receiving.ReceivingNumber}-{item.Id}",
                    $"Purchase receiving item; batch={item.BatchNumber ?? "not-specified"}"),
                creator, reason);
            var result = inventory.Approve(movement.Id, approver, reason);
            results.Add(result);
            item.AddedToStock = true;
            item.StockAddedDate = DateTime.UtcNow;
            item.StockMovementId = movement.Id;
            var orderItem = orderItems[item.PurchaseOrderItemId];
            orderItem.ReceivedQuantity += accepted;
            orderItem.RemainingQuantity = orderItem.Quantity - orderItem.ReceivedQuantity;
            orderItem.UpdatedAt = DateTime.UtcNow;
        }

        receiving.ApprovedBy = approver.Trim();
        receiving.ApprovedDate = DateTime.UtcNow;
        receiving.UpdatedBy = approver.Trim();
        receiving.UpdatedAt = DateTime.UtcNow;
        if (receiving.PurchaseOrder.Items.All(value => value.ReceivedQuantity >= value.Quantity))
        {
            receiving.PurchaseOrder.IsReceived = true;
            receiving.PurchaseOrder.ActualDeliveryDate ??= receiving.ReceivingDate;
            if (receiving.PurchaseOrder.Status != PurchaseOrderStatus.Closed)
                receiving.PurchaseOrder.Status = PurchaseOrderStatus.Received;
        }
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(PurchaseReceiving),
            EntityId = receiving.Id.ToString(),
            Action = "ApproveAndAddToInventory",
            ActorUsername = approver.Trim(),
            Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(new
            {
                receiving.ReceivingNumber,
                ItemCount = receiving.Items.Count,
                MovementIds = results.Select(value => value.Movement.Id)
            }),
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        _context.SaveChanges();
        transaction.Commit();
        return new PurchaseReceivingInventoryResult(receiving, results);
    }

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }
}
