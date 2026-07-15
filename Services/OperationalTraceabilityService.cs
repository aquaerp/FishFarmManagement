using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record TraceabilityRoute(
    string? InputLotCode,
    string? InputReceiptReference,
    string CycleName,
    string PondName,
    string? HarvestLotCode,
    string? SalesOrderNumber,
    int? SalesOrderItemId);

public sealed class OperationalTraceabilityService
{
    private sealed record CyclePondKey(int CycleId, int PondId);
    private readonly FishFarmContext _context;

    public OperationalTraceabilityService(FishFarmContext context) => _context = context;

    public TraceabilityLot RegisterReceivedInputLot(int purchaseReceivingItemId, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        if (_context.TraceabilityLots.Any(value => value.PurchaseReceivingItemId == purchaseReceivingItemId))
            throw new InvalidOperationException("This received item already has a traceability lot.");
        var item = _context.PurchaseReceivingItems.AsNoTracking()
            .Include(value => value.PurchaseReceiving).Include(value => value.InventoryItem)
            .Include(value => value.StockMovement)
            .SingleOrDefault(value => value.Id == purchaseReceivingItemId)
            ?? throw new InvalidOperationException("The purchase receipt item does not exist.");
        if (!item.AddedToStock || item.StockMovement is not { IsApproved: true, MovementType: StockMovementType.Purchase })
            throw new InvalidOperationException("Only an approved purchase receipt already added to inventory can create an input lot.");
        if (string.IsNullOrWhiteSpace(item.BatchNumber))
            throw new InvalidOperationException("A supplier batch number is required for traceability.");
        if (item.AcceptedQuantity <= 0m)
            throw new InvalidOperationException("The accepted batch quantity must be positive.");
        var lotCode = $"{item.PurchaseReceiving.ReceivingNumber}/{item.BatchNumber.Trim()}".ToUpperInvariant();
        if (_context.TraceabilityLots.Any(value => value.LotCode == lotCode))
            throw new InvalidOperationException("The batch number is already registered as a traceability lot.");
        var lot = new TraceabilityLot
        {
            LotCode = lotCode,
            Kind = TraceabilityLotKind.Input,
            InitialQuantity = item.AcceptedQuantity,
            Unit = item.InventoryItem.Unit,
            LotDate = item.PurchaseReceiving.ReceivingDate.Date,
            SourceReference = item.PurchaseReceiving.ReceivingNumber,
            InventoryItemId = item.InventoryItemId,
            PurchaseReceivingItemId = item.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim()
        };
        _context.TraceabilityLots.Add(lot);
        _context.SaveChanges();
        AddAudit(nameof(TraceabilityLot), lot.Id, "RegisterReceivedInputLot", actor, reason,
            new { lot.LotCode, lot.InitialQuantity, lot.InventoryItemId, lot.PurchaseReceivingItemId });
        _context.SaveChanges();
        transaction.Commit();
        return lot;
    }

    public TraceabilityAllocation AllocateInputConsumption(long inputLotId, int stockMovementId,
        int productionCycleId, int pondId, decimal quantity, DateTime eventDate, string reference,
        string actor, string reason)
    {
        RequireAllocation(quantity, reference, actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var lot = _context.TraceabilityLots.SingleOrDefault(value => value.Id == inputLotId)
            ?? throw new InvalidOperationException("The input lot does not exist.");
        if (lot.Kind != TraceabilityLotKind.Input || !lot.InventoryItemId.HasValue)
            throw new InvalidOperationException("Only a received input lot can be allocated to production.");
        var movement = _context.StockMovements.AsNoTracking().SingleOrDefault(value => value.Id == stockMovementId)
            ?? throw new InvalidOperationException("The stock movement does not exist.");
        if (!movement.IsApproved || movement.IsRejected || movement.IsCancelled
            || !IsProductionIssue(movement.MovementType) || movement.InventoryItemId != lot.InventoryItemId)
            throw new InvalidOperationException("Consumption requires an approved outbound movement for the same inventory item.");
        EnsureCyclePond(productionCycleId, pondId, eventDate);
        var lotAllocated = _context.TraceabilityAllocations
            .Where(value => value.AllocationType == TraceabilityAllocationType.InputConsumption && value.SourceLotId == lot.Id)
            .Select(value => value.Quantity).ToArray().Sum();
        if (lotAllocated + quantity > lot.InitialQuantity)
            throw new InvalidOperationException("The allocation exceeds the remaining input-lot quantity.");
        var movementAllocated = _context.TraceabilityAllocations
            .Where(value => value.AllocationType == TraceabilityAllocationType.InputConsumption && value.StockMovementId == stockMovementId)
            .Select(value => value.Quantity).ToArray().Sum();
        if (movementAllocated + quantity > movement.Quantity)
            throw new InvalidOperationException("The allocation exceeds the approved stock-movement quantity.");
        var allocation = NewAllocation(TraceabilityAllocationType.InputConsumption, lot.Id, quantity,
            eventDate, reference, actor);
        allocation.ProductionCycleId = productionCycleId;
        allocation.PondId = pondId;
        allocation.StockMovementId = stockMovementId;
        SaveAllocation(allocation, actor, reason);
        transaction.Commit();
        return allocation;
    }

    public TraceabilityLot RegisterHarvestLot(string lotCode, int productionCycleId, int pondId,
        decimal quantity, DateTime harvestDate, string reference, string actor, string reason)
    {
        RequireAllocation(quantity, reference, actor, reason);
        if (string.IsNullOrWhiteSpace(lotCode) || lotCode.Trim().Length > 120)
            throw new InvalidOperationException("A harvest lot code of at most 120 characters is required.");
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var cycle = EnsureCyclePond(productionCycleId, pondId, harvestDate);
        if (cycle.Status is CycleStatus.Cancelled or CycleStatus.Planning or CycleStatus.Planned)
            throw new InvalidOperationException("Harvest can only be traced for an active or completed production cycle.");
        if (!cycle.TotalHarvestWeight.HasValue || cycle.TotalHarvestWeight <= 0m)
            throw new InvalidOperationException("The production cycle must have an established positive total harvest weight.");
        var harvested = _context.TraceabilityLots.Where(value => value.Kind == TraceabilityLotKind.Harvest
            && value.ProductionCycleId == productionCycleId).Select(value => value.InitialQuantity).ToArray().Sum();
        if (harvested + quantity > cycle.TotalHarvestWeight.Value)
            throw new InvalidOperationException("Registered harvest lots exceed the cycle total harvest weight.");
        var normalizedCode = lotCode.Trim().ToUpperInvariant();
        if (_context.TraceabilityLots.Any(value => value.LotCode == normalizedCode))
            throw new InvalidOperationException("The harvest lot code is already registered.");
        var lot = new TraceabilityLot
        {
            LotCode = normalizedCode, Kind = TraceabilityLotKind.Harvest,
            InitialQuantity = quantity, Unit = "kg", LotDate = harvestDate.Date,
            SourceReference = reference.Trim(), ProductionCycleId = productionCycleId, PondId = pondId,
            CreatedAtUtc = DateTime.UtcNow, CreatedBy = actor.Trim()
        };
        _context.TraceabilityLots.Add(lot);
        _context.SaveChanges();
        AddAudit(nameof(TraceabilityLot), lot.Id, "RegisterHarvestLot", actor, reason,
            new { lot.LotCode, lot.InitialQuantity, lot.ProductionCycleId, lot.PondId });
        _context.SaveChanges();
        transaction.Commit();
        return lot;
    }

    public TraceabilityAllocation AllocateHarvestSale(long harvestLotId, int salesOrderItemId,
        decimal quantity, DateTime eventDate, string reference, string actor, string reason)
    {
        RequireAllocation(quantity, reference, actor, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var lot = _context.TraceabilityLots.SingleOrDefault(value => value.Id == harvestLotId)
            ?? throw new InvalidOperationException("The harvest lot does not exist.");
        if (lot.Kind != TraceabilityLotKind.Harvest || !lot.ProductionCycleId.HasValue)
            throw new InvalidOperationException("Only a harvest lot can be allocated to a sale.");
        var orderItem = _context.SalesOrderItems.AsNoTracking().Include(value => value.SalesOrder)
            .SingleOrDefault(value => value.Id == salesOrderItemId)
            ?? throw new InvalidOperationException("The sales order item does not exist.");
        if (orderItem.SalesOrder.Status != SalesOrderStatus.Completed)
            throw new InvalidOperationException("Only a completed sale can consume a harvest lot.");
        if (orderItem.ProductionCycleId != lot.ProductionCycleId)
            throw new InvalidOperationException("The sales item production cycle does not match the harvest lot.");
        var lotAllocated = _context.TraceabilityAllocations
            .Where(value => value.AllocationType == TraceabilityAllocationType.HarvestSale && value.SourceLotId == lot.Id)
            .Select(value => value.Quantity).ToArray().Sum();
        if (lotAllocated + quantity > lot.InitialQuantity)
            throw new InvalidOperationException("The sale allocation exceeds the remaining harvest-lot quantity.");
        var saleAllocated = _context.TraceabilityAllocations
            .Where(value => value.AllocationType == TraceabilityAllocationType.HarvestSale && value.SalesOrderItemId == salesOrderItemId)
            .Select(value => value.Quantity).ToArray().Sum();
        if (saleAllocated + quantity > orderItem.Quantity)
            throw new InvalidOperationException("The allocations exceed the sales-order item quantity.");
        var allocation = NewAllocation(TraceabilityAllocationType.HarvestSale, lot.Id, quantity,
            eventDate, reference, actor);
        allocation.SalesOrderItemId = salesOrderItemId;
        SaveAllocation(allocation, actor, reason);
        transaction.Commit();
        return allocation;
    }

    public IReadOnlyList<TraceabilityRoute> TraceForward(string inputLotCode)
    {
        var code = NormalizeCode(inputLotCode);
        var input = _context.TraceabilityLots.AsNoTracking().SingleOrDefault(value => value.LotCode == code && value.Kind == TraceabilityLotKind.Input)
            ?? throw new InvalidOperationException("The input lot does not exist.");
        var keys = _context.TraceabilityAllocations.AsNoTracking()
            .Where(value => value.SourceLotId == input.Id && value.AllocationType == TraceabilityAllocationType.InputConsumption)
            .Select(value => new { value.ProductionCycleId, value.PondId }).Distinct().ToArray()
            .Select(value => new CyclePondKey(value.ProductionCycleId!.Value, value.PondId!.Value));
        return BuildRoutes(keys, input).ToArray();
    }

    public IReadOnlyList<TraceabilityRoute> TraceBackward(int salesOrderItemId)
    {
        var harvests = _context.TraceabilityAllocations.AsNoTracking().Include(value => value.SourceLot)
            .Where(value => value.SalesOrderItemId == salesOrderItemId && value.AllocationType == TraceabilityAllocationType.HarvestSale)
            .Select(value => value.SourceLot).ToArray();
        if (harvests.Length == 0) throw new InvalidOperationException("The sales item has no traceable harvest allocation.");
        var routes = new List<TraceabilityRoute>();
        foreach (var harvest in harvests)
        {
            var inputs = _context.TraceabilityAllocations.AsNoTracking().Include(value => value.SourceLot)
                .Where(value => value.AllocationType == TraceabilityAllocationType.InputConsumption
                    && value.ProductionCycleId == harvest.ProductionCycleId && value.PondId == harvest.PondId)
                .Select(value => value.SourceLot).Distinct().ToArray();
            routes.AddRange(BuildSaleRoutes(harvest, salesOrderItemId, inputs));
        }
        return routes;
    }

    private IEnumerable<TraceabilityRoute> BuildRoutes(IEnumerable<CyclePondKey> cyclePonds, TraceabilityLot input)
    {
        foreach (var link in cyclePonds)
        {
            var cycleId = link.CycleId;
            var pondId = link.PondId;
            var harvests = _context.TraceabilityLots.AsNoTracking().Where(value => value.Kind == TraceabilityLotKind.Harvest
                && value.ProductionCycleId == cycleId && value.PondId == pondId).ToArray();
            if (harvests.Length == 0)
            {
                yield return Route(input, cycleId, pondId, null, null, null);
                continue;
            }
            foreach (var harvest in harvests)
            {
                var sales = _context.TraceabilityAllocations.AsNoTracking().Include(value => value.SalesOrderItem).ThenInclude(value => value!.SalesOrder)
                    .Where(value => value.SourceLotId == harvest.Id && value.AllocationType == TraceabilityAllocationType.HarvestSale).ToArray();
                if (sales.Length == 0) yield return Route(input, cycleId, pondId, harvest, null, null);
                foreach (var sale in sales)
                    yield return Route(input, cycleId, pondId, harvest, sale.SalesOrderItem!.SalesOrder.OrderNumber, sale.SalesOrderItemId);
            }
        }
    }

    private IEnumerable<TraceabilityRoute> BuildSaleRoutes(TraceabilityLot harvest, int salesOrderItemId, TraceabilityLot[] inputs)
    {
        var item = _context.SalesOrderItems.AsNoTracking().Include(value => value.SalesOrder).Single(value => value.Id == salesOrderItemId);
        if (inputs.Length == 0) yield return Route(null, harvest.ProductionCycleId!.Value, harvest.PondId!.Value, harvest, item.SalesOrder.OrderNumber, item.Id);
        foreach (var input in inputs)
            yield return Route(input, harvest.ProductionCycleId!.Value, harvest.PondId!.Value, harvest, item.SalesOrder.OrderNumber, item.Id);
    }

    private TraceabilityRoute Route(TraceabilityLot? input, int cycleId, int pondId, TraceabilityLot? harvest,
        string? orderNumber, int? salesItemId)
    {
        var cycle = _context.ProductionCycles.AsNoTracking().Single(value => value.Id == cycleId);
        var pond = _context.Ponds.AsNoTracking().Single(value => value.Id == pondId);
        return new TraceabilityRoute(input?.LotCode, input?.SourceReference, cycle.Name, pond.Name,
            harvest?.LotCode, orderNumber, salesItemId);
    }

    private ProductionCycle EnsureCyclePond(int cycleId, int pondId, DateTime eventDate)
    {
        var link = _context.ProductionCyclePonds.AsNoTracking().Include(value => value.ProductionCycle)
            .SingleOrDefault(value => value.ProductionCycleId == cycleId && value.PondId == pondId)
            ?? throw new InvalidOperationException("The pond is not assigned to the production cycle.");
        if (eventDate.Date < link.StartDate.Date || (link.EndDate.HasValue && eventDate.Date > link.EndDate.Value.Date))
            throw new InvalidOperationException("The traceability event is outside the cycle-pond assignment dates.");
        return link.ProductionCycle;
    }

    private static bool IsProductionIssue(StockMovementType type) => type is StockMovementType.Consumption
        or StockMovementType.Damage or StockMovementType.Expiry or StockMovementType.Expired
        or StockMovementType.Loss or StockMovementType.Waste;

    private static TraceabilityAllocation NewAllocation(TraceabilityAllocationType type, long lotId,
        decimal quantity, DateTime date, string reference, string actor) => new()
    {
        AllocationType = type, SourceLotId = lotId, Quantity = quantity, EventDate = date.Date,
        Reference = reference.Trim(), CreatedAtUtc = DateTime.UtcNow, CreatedBy = actor.Trim()
    };

    private void SaveAllocation(TraceabilityAllocation allocation, string actor, string reason)
    {
        _context.TraceabilityAllocations.Add(allocation);
        _context.SaveChanges();
        AddAudit(nameof(TraceabilityAllocation), allocation.Id, allocation.AllocationType.ToString(), actor, reason,
            new { allocation.SourceLotId, allocation.Quantity, allocation.ProductionCycleId, allocation.PondId,
                allocation.StockMovementId, allocation.SalesOrderItemId, allocation.Reference });
        _context.SaveChanges();
    }

    private void AddAudit(string entityType, long id, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow, EntityType = entityType, EntityId = id.ToString(), Action = action,
            ActorUsername = actor.Trim(), Reason = reason.Trim(), AfterJson = JsonSerializer.Serialize(details),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static void RequireAllocation(decimal quantity, string reference, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        if (quantity <= 0m || decimal.Round(quantity, 3) != quantity)
            throw new InvalidOperationException("Traceability quantity must be positive with no more than three decimal places.");
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 100)
            throw new InvalidOperationException("A traceability reference of at most 100 characters is required.");
    }

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }

    private static string NormalizeCode(string code) => string.IsNullOrWhiteSpace(code)
        ? throw new InvalidOperationException("A lot code is required.") : code.Trim().ToUpperInvariant();
}
