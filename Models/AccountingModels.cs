namespace FishFarmManager.Models;

public enum LedgerAccountType { Asset, Liability, Equity, Revenue, Expense }
public enum AccountNormalBalance { Debit, Credit }
public enum FinancialStatementCategory
{
    Unclassified,
    Cash,
    AccountsReceivable,
    Inventory,
    OtherCurrentAsset,
    FixedAssetCost,
    AccumulatedDepreciation,
    OtherNonCurrentAsset,
    AccountsPayable,
    SalariesPayable,
    TaxesPayable,
    OtherCurrentLiability,
    LongTermLoan,
    OtherLongTermLiability,
    Capital,
    RetainedEarnings,
    OtherEquity,
    SalesRevenue,
    OtherRevenue,
    CostOfGoodsSold,
    SalariesExpense,
    DepreciationExpense,
    UtilitiesExpense,
    MaintenanceExpense,
    OtherOperatingExpense,
    InterestIncome,
    InterestExpense,
    IncomeTaxExpense
}
public enum FiscalPeriodStatus { Open, Closed }
public enum JournalEntryStatus { Draft, Approved, Posted, Reversed }
public enum CostCenterType { Farm, Pond, ProductionCycle, Department, Other }
public enum AccountingConfigurationStatus { Draft, Approved, Retired }
public enum AccountingAdjustmentType { Accrual, Deferral, Reclassification, Estimate, Correction, Other }
public enum ExchangeRateStatus { Draft, Approved, Retired }
public enum ExchangeRatePurpose { Transaction, Closing }
public enum PostingEventType
{
    SalesCompleted,
    PurchaseReceived,
    CustomerPaymentReceived,
    SupplierPaymentCompleted,
    PayrollApproved,
    DepreciationApproved
}
public enum PostingComponent
{
    AccountsReceivable,
    SalesRevenue,
    OutputVat,
    InventoryOrExpense,
    InputVat,
    AccountsPayable,
    Cash,
    PayrollExpense,
    SalariesPayable,
    PayrollWithholdingsPayable,
    DepreciationExpense,
    AccumulatedDepreciation
}

public sealed class LedgerAccount
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public LedgerAccountType Type { get; set; }
    public AccountNormalBalance NormalBalance { get; set; }
    public FinancialStatementCategory FinancialStatementCategory { get; set; }
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
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }
    public long? OpeningBalanceJournalEntryId { get; set; }
    public long? ClosingJournalEntryId { get; set; }
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
    public string? ForeignCurrencyCode { get; set; }
    public decimal? ForeignAmount { get; set; }
    public long? ForeignExchangeRateId { get; set; }
    public ForeignExchangeRate? ForeignExchangeRate { get; set; }
    public decimal? ExchangeRateSarPerUnit { get; set; }
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

public sealed class AccountingConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public AccountingConfigurationStatus Status { get; set; } = AccountingConfigurationStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public ICollection<PostingMapping> PostingMappings { get; set; } = new List<PostingMapping>();
}

public sealed class PostingMapping
{
    public int Id { get; set; }
    public int AccountingConfigurationId { get; set; }
    public AccountingConfiguration AccountingConfiguration { get; set; } = null!;
    public PostingEventType EventType { get; set; }
    public PostingComponent Component { get; set; }
    public int LedgerAccountId { get; set; }
    public LedgerAccount LedgerAccount { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public sealed class OperationalPostingRecord
{
    public long Id { get; set; }
    public PostingEventType EventType { get; set; }
    public string SourceEntityType { get; set; } = string.Empty;
    public string SourceEntityId { get; set; } = string.Empty;
    public long JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class AccountingAdjustment
{
    public long Id { get; set; }
    public long JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public AccountingAdjustmentType Type { get; set; }
    public string SupportingDocumentReference { get; set; } = string.Empty;
    public DateTime? ScheduledReversalDate { get; set; }
    public long? ReversalJournalEntryId { get; set; }
    public JournalEntry? ReversalJournalEntry { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class ForeignExchangeRate
{
    public long Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime RateDate { get; set; }
    public ExchangeRatePurpose Purpose { get; set; }
    public int Version { get; set; }
    public decimal SarPerUnit { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public ExchangeRateStatus Status { get; set; } = ExchangeRateStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
}
