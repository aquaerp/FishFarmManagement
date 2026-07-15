using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed class InventoryGeneralLedgerReconciliationService
{
    private readonly FishFarmContext _context;

    public InventoryGeneralLedgerReconciliationService(FishFarmContext context) => _context = context;

    public InventoryLedgerReconciliation Run(DateTime asOfDate, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        var asOf = asOfDate.Date;
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var items = _context.InventoryItems.AsNoTracking().OrderBy(value => value.Id).ToArray();
        var approvedMovements = _context.StockMovements.AsNoTracking()
            .Where(value => value.IsApproved && !value.IsRejected && !value.IsCancelled
                && value.MovementDate.Date <= asOf)
            .OrderBy(value => value.ApprovedAt ?? value.ApprovedDate ?? value.CreatedAt).ThenBy(value => value.Id).ToArray();
        var valuations = _context.InventoryValuations.AsNoTracking()
            .Where(value => value.IsApproved && value.ValuationDate.Date <= asOf)
            .OrderBy(value => value.ApprovedDate ?? value.CreatedDate).ThenBy(value => value.Id).ToArray();

        var evidence = items.Select(item =>
        {
            var movement = approvedMovements.LastOrDefault(value => value.InventoryItemId == item.Id);
            var valuation = valuations.LastOrDefault(value => value.InventoryItemId == item.Id);
            var expectedQuantity = movement?.BalanceAfter ?? 0m;
            var masterValue = decimal.Round(item.CurrentStock * item.UnitCost, 2, MidpointRounding.AwayFromZero);
            var valuationValue = valuation?.TotalValue ?? 0m;
            return new
            {
                item.Id,
                item.Name,
                MasterQuantity = item.CurrentStock,
                MovementQuantity = expectedQuantity,
                QuantityDifference = item.CurrentStock - expectedQuantity,
                MasterValue = masterValue,
                ValuationValue = valuationValue,
                ValuationDifference = masterValue - valuationValue,
                LastMovementId = movement?.Id,
                LastValuationId = valuation?.Id
            };
        }).ToArray();

        var quantityExceptions = evidence.Count(value => value.QuantityDifference != 0m);
        var valuationExceptions = evidence.Count(value => Math.Abs(value.ValuationDifference) > 0.01m);
        var inventorySubledger = evidence.Sum(value => value.MasterValue);
        var inventoryAccountId = ResolveMappedAccount(PostingComponent.InventoryAsset);
        var workInProgressAccountId = ResolveMappedAccount(PostingComponent.WorkInProgressInventory);
        var inventoryGl = AccountBalance(inventoryAccountId, asOf);
        var workInProgressGl = AccountBalance(workInProgressAccountId, asOf);
        var costEvents = _context.ProductionCostEvents.AsNoTracking()
            .Where(value => value.EventDate.Date <= asOf).ToArray();
        var workInProgressSubledger = decimal.Round(
            costEvents.Where(value => value.EventType is ProductionCostEventType.InputConsumption
                    or ProductionCostEventType.DirectCost).Sum(value => value.Amount)
            - costEvents.Where(value => value.EventType is ProductionCostEventType.AbnormalMortality
                    or ProductionCostEventType.HarvestCapitalization).Sum(value => value.Amount),
            2, MidpointRounding.AwayFromZero);
        if (workInProgressSubledger < 0m)
            throw new InvalidOperationException("The production cost subledger has a negative work-in-progress balance.");
        var inventoryDifference = decimal.Round(inventorySubledger - inventoryGl, 2, MidpointRounding.AwayFromZero);
        var workInProgressDifference = decimal.Round(workInProgressSubledger - workInProgressGl, 2,
            MidpointRounding.AwayFromZero);
        var passed = quantityExceptions == 0 && valuationExceptions == 0
            && Math.Abs(inventoryDifference) <= 0.01m && Math.Abs(workInProgressDifference) <= 0.01m;
        var run = new InventoryLedgerReconciliation
        {
            AsOfDate = asOf,
            InventoryItemCount = items.Length,
            QuantityExceptionCount = quantityExceptions,
            ValuationExceptionCount = valuationExceptions,
            InventorySubledgerValue = inventorySubledger,
            InventoryGeneralLedgerValue = inventoryGl,
            InventoryDifference = inventoryDifference,
            WorkInProgressSubledgerValue = workInProgressSubledger,
            WorkInProgressGeneralLedgerValue = workInProgressGl,
            WorkInProgressDifference = workInProgressDifference,
            IsPassed = passed,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                Items = evidence,
                InventoryAccountId = inventoryAccountId,
                WorkInProgressAccountId = workInProgressAccountId,
                ProductionCostEventCount = costEvents.Length
            }),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            Reason = reason.Trim()
        };
        _context.InventoryLedgerReconciliations.Add(run);
        _context.SaveChanges();
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(InventoryLedgerReconciliation),
            EntityId = run.Id.ToString(),
            Action = passed ? "Passed" : "Failed",
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(new
            {
                run.AsOfDate,
                run.QuantityExceptionCount,
                run.ValuationExceptionCount,
                run.InventoryDifference,
                run.WorkInProgressDifference
            }),
            CorrelationId = Guid.NewGuid().ToString("N")
        });
        _context.SaveChanges();
        transaction.Commit();
        return run;
    }

    private int ResolveMappedAccount(PostingComponent component)
    {
        var configuration = _context.AccountingConfigurations.AsNoTracking()
            .Where(value => value.Status == AccountingConfigurationStatus.Approved)
            .OrderByDescending(value => value.Version).FirstOrDefault()
            ?? throw new InvalidOperationException("No approved accounting configuration is available for reconciliation.");
        return _context.PostingMappings.AsNoTracking()
            .Where(value => value.AccountingConfigurationId == configuration.Id
                && value.EventType == PostingEventType.ProductionCostEventApproved
                && value.Component == component && value.IsActive)
            .Select(value => value.LedgerAccountId).SingleOrDefault() is var accountId && accountId != 0
            ? accountId
            : throw new InvalidOperationException($"The approved posting map is missing {component}.");
    }

    private decimal AccountBalance(int accountId, DateTime asOfDate)
    {
        var lines = _context.JournalEntryLines.AsNoTracking()
            .Include(value => value.JournalEntry)
            .Where(value => value.LedgerAccountId == accountId
                && value.JournalEntry.EntryDate.Date <= asOfDate
                && (value.JournalEntry.Status == JournalEntryStatus.Posted
                    || value.JournalEntry.Status == JournalEntryStatus.Reversed))
            .Select(value => new { value.Debit, value.Credit }).ToArray();
        return decimal.Round(lines.Sum(value => value.Debit) - lines.Sum(value => value.Credit),
            2, MidpointRounding.AwayFromZero);
    }

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }
}
