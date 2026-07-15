namespace FishFarmManager.Models;

public enum TraceabilityLotKind { Input, Harvest }
public enum TraceabilityAllocationType { InputConsumption, HarvestSale }

public sealed class TraceabilityLot
{
    public long Id { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public TraceabilityLotKind Kind { get; set; }
    public decimal InitialQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime LotDate { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public int? PurchaseReceivingItemId { get; set; }
    public PurchaseReceivingItem? PurchaseReceivingItem { get; set; }
    public int? ProductionCycleId { get; set; }
    public ProductionCycle? ProductionCycle { get; set; }
    public int? PondId { get; set; }
    public Pond? Pond { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ICollection<TraceabilityAllocation> Allocations { get; set; } = new List<TraceabilityAllocation>();
}

public sealed class TraceabilityAllocation
{
    public long Id { get; set; }
    public TraceabilityAllocationType AllocationType { get; set; }
    public long SourceLotId { get; set; }
    public TraceabilityLot SourceLot { get; set; } = null!;
    public decimal Quantity { get; set; }
    public DateTime EventDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int? ProductionCycleId { get; set; }
    public ProductionCycle? ProductionCycle { get; set; }
    public int? PondId { get; set; }
    public Pond? Pond { get; set; }
    public int? StockMovementId { get; set; }
    public StockMovement? StockMovement { get; set; }
    public int? SalesOrderItemId { get; set; }
    public SalesOrderItem? SalesOrderItem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
