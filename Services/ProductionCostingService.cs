using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record HarvestCostingResult(
    ProductionCostEvent CostEvent,
    StockMovement StockMovement,
    decimal CostPerKg,
    decimal RemainingWorkInProgress);

public sealed class ProductionCostingService
{
    private readonly FishFarmContext _context;

    public ProductionCostingService(FishFarmContext context) => _context = context;

    public PondProductionCostBalance GetPondBalance(int cycleId, int pondId)
    {
        var events = _context.ProductionCostEvents.AsNoTracking()
            .Where(value => value.ProductionCycleId == cycleId
                && (value.PondId == pondId || value.DestinationPondId == pondId))
            .ToArray();
        var charged = events.Where(value => value.PondId == pondId
                && value.EventType is ProductionCostEventType.InputConsumption or ProductionCostEventType.DirectCost)
            .Sum(value => value.Amount);
        var abnormal = events.Where(value => value.PondId == pondId
                && value.EventType == ProductionCostEventType.AbnormalMortality).Sum(value => value.Amount);
        var transfersOut = events.Where(value => value.PondId == pondId
                && value.EventType == ProductionCostEventType.PondTransfer).Sum(value => value.Amount);
        var transfersIn = events.Where(value => value.DestinationPondId == pondId
                && value.EventType == ProductionCostEventType.PondTransfer).Sum(value => value.Amount);
        var harvested = events.Where(value => value.PondId == pondId
                && value.EventType == ProductionCostEventType.HarvestCapitalization).Sum(value => value.Amount);
        var balance = decimal.Round(charged + transfersIn - abnormal - transfersOut - harvested, 2,
            MidpointRounding.AwayFromZero);
        if (balance < 0m)
            throw new InvalidOperationException("The production cost ledger has a negative work-in-progress balance.");
        return new PondProductionCostBalance(cycleId, pondId, charged, abnormal, transfersIn, transfersOut, harvested, balance);
    }

    public ProductionCostEvent CapitalizeInputConsumption(int stockMovementId, int pondId, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var movement = _context.StockMovements.AsNoTracking().SingleOrDefault(value => value.Id == stockMovementId)
            ?? throw new InvalidOperationException("The inventory consumption movement does not exist.");
        if (!movement.IsApproved || movement.IsRejected || movement.IsCancelled
            || movement.MovementType != StockMovementType.Consumption || movement.TotalCost <= 0m)
            throw new InvalidOperationException("Only an approved, valued consumption movement can enter production cost.");
        if (_context.OperationalPostingRecords.Any(value => value.EventType == PostingEventType.InventoryMovementApproved
            && value.SourceEntityType == nameof(StockMovement) && value.SourceEntityId == movement.Id.ToString()))
            throw new InvalidOperationException(
                "This consumption was already posted as an operating expense; reverse it before production capitalization.");
        var allocations = _context.TraceabilityAllocations.AsNoTracking()
            .Where(value => value.StockMovementId == stockMovementId && value.PondId == pondId
                && value.AllocationType == TraceabilityAllocationType.InputConsumption).ToArray();
        if (allocations.Length == 0 || allocations.Any(value => value.ProductionCycleId != allocations[0].ProductionCycleId))
            throw new InvalidOperationException("The consumption movement must be consistently traceable to this cycle and pond.");
        var cycleId = allocations[0].ProductionCycleId
            ?? throw new InvalidOperationException("The consumption allocation has no production cycle.");
        var allocatedQuantity = allocations.Sum(value => value.Quantity);
        var allocatedAmount = decimal.Round(movement.TotalCost * allocatedQuantity / movement.Quantity, 2,
            MidpointRounding.AwayFromZero);
        EnsureCyclePond(cycleId, pondId, movement.MovementDate);
        var entry = AddEvent(ProductionCostEventType.InputConsumption, cycleId, pondId,
            allocatedQuantity, allocatedAmount, movement.MovementDate, movement.Reference ?? $"STOCK-{movement.Id}",
            nameof(StockMovement), $"{movement.Id}:{pondId}", actor,
            $"Approved weighted-average inventory issue allocation: {allocatedQuantity:N3} x {movement.UnitCost:N2}",
            stockMovementId: movement.Id);
        _context.SaveChanges();
        AddAudit(entry, actor, reason);
        UpdateCycleTotal(entry.ProductionCycleId);
        _context.SaveChanges();
        transaction.Commit();
        return entry;
    }

    public ProductionCostEvent CapitalizeDirectCost(int costRecordId, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var cost = _context.CostRecords.AsNoTracking().SingleOrDefault(value => value.Id == costRecordId)
            ?? throw new InvalidOperationException("The direct cost record does not exist.");
        if (!cost.ProductionCycleId.HasValue || !cost.PondId.HasValue || cost.Amount <= 0m
            || decimal.Round(cost.Amount, 2) != cost.Amount)
            throw new InvalidOperationException("A positive two-decimal direct cost assigned to a cycle and pond is required.");
        EnsureCyclePond(cost.ProductionCycleId.Value, cost.PondId.Value, cost.Date);
        var entry = AddEvent(ProductionCostEventType.DirectCost, cost.ProductionCycleId.Value, cost.PondId.Value,
            cost.Quantity ?? 0m, cost.Amount, cost.Date, cost.DocumentNumber ?? $"COST-{cost.Id}",
            nameof(CostRecord), cost.Id.ToString(), actor, $"Direct production cost: {cost.Description}", costRecordId: cost.Id);
        _context.SaveChanges();
        AddAudit(entry, actor, reason);
        UpdateCycleTotal(entry.ProductionCycleId);
        _context.SaveChanges();
        transaction.Commit();
        return entry;
    }

    public ProductionCostEvent CostMortality(int mortalityRecordId, int pondId, decimal liveBiomassBeforeKg,
        bool abnormal, string reference, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var mortality = _context.MortalityRecords.AsNoTracking().SingleOrDefault(value => value.Id == mortalityRecordId)
            ?? throw new InvalidOperationException("The mortality record does not exist.");
        if (_context.ProductionCostEvents.Any(value => value.MortalityRecordId == mortalityRecordId))
            throw new InvalidOperationException("This mortality record has already been costed.");
        if (mortality.Status is not (MortalityStatus.Analyzed or MortalityStatus.Closed))
            throw new InvalidOperationException("Mortality must be analyzed or closed before costing.");
        if (mortality.DeadFishCount <= 0 || !mortality.AverageWeight.HasValue || mortality.AverageWeight <= 0)
            throw new InvalidOperationException("Mortality count and average weight are required for biomass costing.");
        var deadBiomass = decimal.Round(mortality.DeadFishCount * (decimal)mortality.AverageWeight.Value, 3,
            MidpointRounding.AwayFromZero);
        if (liveBiomassBeforeKg < deadBiomass || liveBiomassBeforeKg <= 0m)
            throw new InvalidOperationException("Biomass before mortality must include and cover the dead biomass.");
        EnsureCyclePond(mortality.CycleId, pondId, mortality.Date);
        var balance = GetPondBalance(mortality.CycleId, pondId).WorkInProgressBalance;
        var loss = abnormal
            ? decimal.Round(balance * deadBiomass / liveBiomassBeforeKg, 2, MidpointRounding.AwayFromZero)
            : 0m;
        if (abnormal && (loss <= 0m || loss > balance))
            throw new InvalidOperationException("Abnormal mortality cannot be valued from the available work in progress.");
        var entry = AddEvent(abnormal ? ProductionCostEventType.AbnormalMortality : ProductionCostEventType.NormalMortality,
            mortality.CycleId, pondId, deadBiomass, loss, mortality.Date, reference,
            nameof(MortalityRecord), mortality.Id.ToString(), actor,
            abnormal
                ? $"Abnormal loss allocated by dead biomass {deadBiomass:N3} / biomass before {liveBiomassBeforeKg:N3}."
                : "Normal mortality: cost remains in work in progress and is absorbed by surviving production.",
            mortalityRecordId: mortality.Id);
        _context.SaveChanges();
        AddAudit(entry, actor, reason);
        UpdateCycleTotal(entry.ProductionCycleId);
        _context.SaveChanges();
        transaction.Commit();
        return entry;
    }

    public ProductionCostEvent TransferPondCost(int cycleId, int sourcePondId, int destinationPondId,
        decimal transferredBiomassKg, decimal sourceBiomassBeforeKg, DateTime date, string reference,
        string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        if (sourcePondId == destinationPondId || transferredBiomassKg <= 0m
            || sourceBiomassBeforeKg < transferredBiomassKg)
            throw new InvalidOperationException("A valid transfer quantity, source biomass, and different destination pond are required.");
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        EnsureCyclePond(cycleId, sourcePondId, date);
        EnsureCyclePond(cycleId, destinationPondId, date);
        var sourceBalance = GetPondBalance(cycleId, sourcePondId).WorkInProgressBalance;
        var amount = decimal.Round(sourceBalance * transferredBiomassKg / sourceBiomassBeforeKg, 2,
            MidpointRounding.AwayFromZero);
        if (amount <= 0m || amount > sourceBalance)
            throw new InvalidOperationException("The transfer cannot be valued from the source pond work in progress.");
        var entry = AddEvent(ProductionCostEventType.PondTransfer, cycleId, sourcePondId, transferredBiomassKg,
            amount, date, reference, nameof(ProductionCyclePond), reference, actor,
            $"Proportional WIP transfer: {transferredBiomassKg:N3} / {sourceBiomassBeforeKg:N3} kg.",
            destinationPondId: destinationPondId);
        _context.SaveChanges();
        AddAudit(entry, actor, reason);
        _context.SaveChanges();
        transaction.Commit();
        return entry;
    }

    public HarvestCostingResult CapitalizeHarvest(long harvestLotId, int outputInventoryItemId,
        decimal harvestableBiomassBeforeKg, bool finalHarvest, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var lot = _context.TraceabilityLots.AsNoTracking().SingleOrDefault(value => value.Id == harvestLotId)
            ?? throw new InvalidOperationException("The harvest traceability lot does not exist.");
        if (lot.Kind != TraceabilityLotKind.Harvest || !lot.ProductionCycleId.HasValue || !lot.PondId.HasValue)
            throw new InvalidOperationException("Only a harvest traceability lot can be capitalized.");
        var item = _context.InventoryItems.SingleOrDefault(value => value.Id == outputInventoryItemId && value.IsActive)
            ?? throw new InvalidOperationException("The active output inventory item does not exist.");
        if (item.Category is not (InventoryCategory.FreshFish or InventoryCategory.FrozenFish))
            throw new InvalidOperationException("Harvest output must use a fresh or frozen fish inventory item.");
        if (harvestableBiomassBeforeKg < lot.InitialQuantity || harvestableBiomassBeforeKg <= 0m)
            throw new InvalidOperationException("Harvestable biomass before harvest must cover the harvest lot quantity.");
        EnsureCyclePond(lot.ProductionCycleId.Value, lot.PondId.Value, lot.LotDate);
        var balanceBefore = GetPondBalance(lot.ProductionCycleId.Value, lot.PondId.Value).WorkInProgressBalance;
        var amount = finalHarvest
            ? balanceBefore
            : decimal.Round(balanceBefore * lot.InitialQuantity / harvestableBiomassBeforeKg, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0m || amount > balanceBefore)
            throw new InvalidOperationException("The harvest cannot be valued from the available work in progress.");
        var unitCost = decimal.Round(amount / lot.InitialQuantity, 2, MidpointRounding.AwayFromZero);
        var inventoryBefore = item.CurrentStock;
        var inventoryValueBefore = decimal.Round(inventoryBefore * item.UnitCost, 2, MidpointRounding.AwayFromZero);
        var inventoryAfter = inventoryBefore + lot.InitialQuantity;
        item.CurrentStock = inventoryAfter;
        item.UnitCost = decimal.Round((inventoryValueBefore + amount) / inventoryAfter, 2, MidpointRounding.AwayFromZero);
        item.LastModifiedDate = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = actor.Trim();
        var movement = new StockMovement
        {
            InventoryItemId = item.Id,
            ProductionCycleId = lot.ProductionCycleId,
            MovementType = StockMovementType.Production,
            MovementDate = lot.LotDate,
            Quantity = lot.InitialQuantity,
            UnitCost = unitCost,
            TotalCost = amount,
            BalanceBefore = inventoryBefore,
            BalanceAfter = inventoryAfter,
            Reference = lot.LotCode,
            ReferenceNumber = lot.SourceReference,
            ReferenceType = nameof(TraceabilityLot),
            ReferenceId = checked((int)lot.Id),
            IsApproved = true,
            ApprovedByUsername = actor.Trim(),
            ApprovalReason = reason.Trim(),
            ApprovedAt = DateTime.UtcNow,
            ApprovedDate = DateTime.UtcNow,
            CreatedBy = actor.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);
        _context.InventoryValuations.Add(new InventoryValuation
        {
            InventoryItemId = item.Id, ValuationDate = lot.LotDate, Method = InventoryTransactionService.CostingMethod,
            Quantity = inventoryAfter, UnitCost = item.UnitCost,
            TotalValue = decimal.Round(inventoryAfter * item.UnitCost, 2, MidpointRounding.AwayFromZero),
            IsApproved = true, ApprovedDate = DateTime.UtcNow, CreatedBy = actor.Trim(), CreatedDate = DateTime.UtcNow,
            Notes = $"Capitalized harvest lot {lot.LotCode}"
        });
        _context.SaveChanges();
        var entry = AddEvent(ProductionCostEventType.HarvestCapitalization, lot.ProductionCycleId.Value,
            lot.PondId.Value, lot.InitialQuantity, amount, lot.LotDate, lot.LotCode,
            nameof(TraceabilityLot), lot.Id.ToString(), actor,
            finalHarvest ? "Final harvest: all remaining pond WIP capitalized." :
                $"Partial harvest allocation: {lot.InitialQuantity:N3} / {harvestableBiomassBeforeKg:N3} kg.",
            stockMovementId: movement.Id, harvestLotId: lot.Id);
        _context.SaveChanges();
        AddAudit(entry, actor, reason);
        UpdateCycleTotal(entry.ProductionCycleId);
        _context.SaveChanges();
        transaction.Commit();
        var remaining = GetPondBalance(entry.ProductionCycleId, entry.PondId).WorkInProgressBalance;
        return new HarvestCostingResult(entry, movement, unitCost, remaining);
    }

    private ProductionCostEvent AddEvent(ProductionCostEventType type, int cycleId, int pondId,
        decimal quantityKg, decimal amount, DateTime date, string reference, string sourceType, string sourceId,
        string actor, string basis, int? destinationPondId = null, int? stockMovementId = null,
        int? mortalityRecordId = null, int? costRecordId = null, long? harvestLotId = null)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 100)
            throw new InvalidOperationException("A production costing reference of up to 100 characters is required.");
        if (quantityKg < 0m || decimal.Round(quantityKg, 3) != quantityKg || amount < 0m || decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException("Production quantities use three decimals and SAR amounts use two decimals.");
        if (_context.ProductionCostEvents.Any(value => value.EventType == type
            && value.SourceEntityType == sourceType && value.SourceEntityId == sourceId))
            throw new InvalidOperationException("This source event has already been included in production costing.");
        var entry = new ProductionCostEvent
        {
            EventType = type, ProductionCycleId = cycleId, PondId = pondId, DestinationPondId = destinationPondId,
            QuantityKg = quantityKg, Amount = amount, EventDate = date.Date, Reference = reference.Trim(),
            SourceEntityType = sourceType, SourceEntityId = sourceId, StockMovementId = stockMovementId,
            MortalityRecordId = mortalityRecordId, CostRecordId = costRecordId, HarvestLotId = harvestLotId,
            MeasurementBasis = basis, CreatedAtUtc = DateTime.UtcNow, CreatedBy = actor.Trim()
        };
        _context.ProductionCostEvents.Add(entry);
        return entry;
    }

    private void EnsureCyclePond(int cycleId, int pondId, DateTime date)
    {
        var valid = _context.ProductionCyclePonds.AsNoTracking().Any(value => value.ProductionCycleId == cycleId
            && value.PondId == pondId && value.StartDate.Date <= date.Date
            && (!value.EndDate.HasValue || value.EndDate.Value.Date >= date.Date));
        if (!valid) throw new InvalidOperationException("The pond is not assigned to the production cycle on the event date.");
    }

    private void UpdateCycleTotal(int cycleId)
    {
        var cycle = _context.ProductionCycles.Single(value => value.Id == cycleId);
        var events = _context.ProductionCostEvents.AsNoTracking().Where(value => value.ProductionCycleId == cycleId).ToArray();
        cycle.TotalCost = decimal.Round(events
            .Where(value => value.EventType is ProductionCostEventType.InputConsumption or ProductionCostEventType.DirectCost)
            .Sum(value => value.Amount) - events.Where(value => value.EventType == ProductionCostEventType.AbnormalMortality)
            .Sum(value => value.Amount), 2, MidpointRounding.AwayFromZero);
        cycle.UpdatedAt = DateTime.UtcNow;
    }

    private void AddAudit(ProductionCostEvent entry, string actor, string reason) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow, EntityType = nameof(ProductionCostEvent), EntityId = entry.Id.ToString(),
            Action = entry.EventType.ToString(), ActorUsername = actor.Trim(), Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(new { entry.ProductionCycleId, entry.PondId, entry.DestinationPondId,
                entry.QuantityKg, entry.Amount, entry.Reference, entry.MeasurementBasis }),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }
}
