namespace FishFarmManager.Models;

public enum LedgerAccountType { Asset, Liability, Equity, Revenue, Expense }
public enum AccountNormalBalance { Debit, Credit }
public enum FiscalPeriodStatus { Open, Closed }
public enum JournalEntryStatus { Draft, Approved, Posted, Reversed }
public enum CostCenterType { Farm, Pond, ProductionCycle, Department, Other }

public sealed class LedgerAccount
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public LedgerAccountType Type { get; set; }
    public AccountNormalBalance NormalBalance { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public bool IsActive { get; set; } = true;
    public bool AllowsPosting { get; set; } = true;
    public int? ParentAccountId { get; set; }
    public LedgerAccount? ParentAccount { get; set; }
    public ICollection<LedgerAccount> Children { get; set; } = new List<LedgerAccount>();
    public ICollection<JournalEntryLine> JournalLines { get; set; } = new List<JournalEntryLine>();
}

public sealed class FiscalYear
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public ICollection<FiscalPeriod> Periods { get; set; } = new List<FiscalPeriod>();
}

public sealed class FiscalPeriod
{
    public int Id { get; set; }
    public int FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}

public sealed class CostCenter
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CostCenterType Type { get; set; }
    public string? ExternalReference { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<JournalEntryLine> JournalLines { get; set; } = new List<JournalEntryLine>();
}

public sealed class JournalEntry
{
    public long Id { get; set; }
    public long SequenceNumber { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public int FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string? PostedBy { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReversedBy { get; set; }
    public long? ReversalOfJournalEntryId { get; set; }
    public JournalEntry? ReversalOfJournalEntry { get; set; }
    public ICollection<JournalEntry> Reversals { get; set; } = new List<JournalEntry>();
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

public sealed class JournalEntryLine
{
    public long Id { get; set; }
    public long JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public int LineNumber { get; set; }
    public int LedgerAccountId { get; set; }
    public LedgerAccount LedgerAccount { get; set; } = null!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
    public string? Description { get; set; }
}

public sealed class AccountingSequence
{
    public string Name { get; set; } = string.Empty;
    public long NextValue { get; set; }
}

public sealed class AccountingAuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActorUsername { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
