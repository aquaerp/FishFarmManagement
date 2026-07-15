namespace FishFarmManager.Models;

public enum ProductionCostEventType
{
    InputConsumption,
    DirectCost,
    NormalMortality,
    AbnormalMortality,
    PondTransfer,
    HarvestCapitalization
}

public sealed class ProductionCostEvent
{
    public long Id { get; set; }
    public ProductionCostEventType EventType { get; set; }
    public int ProductionCycleId { get; set; }
    public ProductionCycle ProductionCycle { get; set; } = null!;
    public int PondId { get; set; }
    public Pond Pond { get; set; } = null!;
    public int? DestinationPondId { get; set; }
    public Pond? DestinationPond { get; set; }
    public decimal QuantityKg { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public int? StockMovementId { get; set; }
    public StockMovement? StockMovement { get; set; }
    public int? MortalityRecordId { get; set; }
    public MortalityRecord? MortalityRecord { get; set; }
    public int? CostRecordId { get; set; }
    public CostRecord? CostRecord { get; set; }
    public long? HarvestLotId { get; set; }
    public TraceabilityLot? HarvestLot { get; set; }
    public DateTime EventDate { get; set; }
    public string MeasurementBasis { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed record PondProductionCostBalance(
    int ProductionCycleId,
    int PondId,
    decimal ChargedCost,
    decimal AbnormalMortalityLoss,
    decimal TransfersIn,
    decimal TransfersOut,
    decimal HarvestedCost,
    decimal WorkInProgressBalance);

public sealed class InventoryLedgerReconciliation
{
    public long Id { get; set; }
    public DateTime AsOfDate { get; set; }
    public int InventoryItemCount { get; set; }
    public int QuantityExceptionCount { get; set; }
    public int ValuationExceptionCount { get; set; }
    public decimal InventorySubledgerValue { get; set; }
    public decimal InventoryGeneralLedgerValue { get; set; }
    public decimal InventoryDifference { get; set; }
    public decimal WorkInProgressSubledgerValue { get; set; }
    public decimal WorkInProgressGeneralLedgerValue { get; set; }
    public decimal WorkInProgressDifference { get; set; }
    public bool IsPassed { get; set; }
    public string EvidenceJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
