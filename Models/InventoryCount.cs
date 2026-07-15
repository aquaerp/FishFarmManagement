namespace FishFarmManager.Models;

public enum InventoryCountType { Periodic, Full }
public enum InventoryCountStatus { Draft, Submitted, Approved, Rejected }

public sealed class InventoryCount
{
    public long Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public InventoryCountType CountType { get; set; }
    public InventoryCountStatus Status { get; set; } = InventoryCountStatus.Draft;
    public DateTime CountDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovalReason { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public ICollection<InventoryCountLine> Lines { get; set; } = new List<InventoryCountLine>();
}

public sealed class InventoryCountLine
{
    public long Id { get; set; }
    public long InventoryCountId { get; set; }
    public InventoryCount InventoryCount { get; set; } = null!;
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public decimal BookQuantitySnapshot { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public decimal? ActualQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
    public decimal VarianceValue { get; set; }
    public string? VarianceReason { get; set; }
    public int? StockMovementId { get; set; }
    public StockMovement? StockMovement { get; set; }
    public long? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
}
