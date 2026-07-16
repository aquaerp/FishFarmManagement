using System.Data;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record OperationalForeignCurrencyMeasurement(
    string CurrencyCode,
    decimal ForeignGrossAmount,
    long ExchangeRateId);

public sealed class OperationalPostingService
{
    private sealed record ComponentPosting(PostingComponent Component, decimal Debit, decimal Credit, string Description);
    private readonly FishFarmContext _context;
    private readonly GeneralLedgerService _ledger;

    public OperationalPostingService(FishFarmContext context)
    {
        _context = context;
        _ledger = new GeneralLedgerService(context);
    }

    public JournalEntry CreateSalesCompletionDraft(int salesOrderId, int fiscalPeriodId, string actor, string reason,
        OperationalForeignCurrencyMeasurement? foreignCurrency = null)
    {
        var order = _context.SalesOrders.AsNoTracking().SingleOrDefault(item => item.Id == salesOrderId)
            ?? throw new InvalidOperationException("The sales order does not exist.");
        if (order.Status != SalesOrderStatus.Completed)
            throw new InvalidOperationException("Only completed sales orders can be transferred to accounting.");
        if (!order.DeliveryDate.HasValue)
            throw new InvalidOperationException("Revenue cannot be recognized before documented delivery and transfer of control.");
        var total = order.TotalAmount > 0 ? order.TotalAmount : order.GrandTotal;
        var vat = order.VATAmount > 0 ? order.VATAmount : order.TaxAmount;
        return CreateOperationalDraft(
            PostingEventType.SalesCompleted,
            nameof(SalesOrder),
            order.Id.ToString(),
            order.OrderDate,
            fiscalPeriodId,
            $"Completed sales order {order.OrderNumber}",
            order.OrderNumber,
            total,
            vat,
            actor,
            reason,
            PostingComponent.AccountsReceivable,
            PostingComponent.SalesRevenue,
            PostingComponent.OutputVat,
            debitPrimary: true,
            foreignCurrency);
    }

    public JournalEntry CreatePurchaseReceiptDraft(int purchaseReceivingId, int fiscalPeriodId, string actor, string reason,
        OperationalForeignCurrencyMeasurement? foreignCurrency = null)
    {
        var receiving = _context.PurchaseReceivings.AsNoTracking().Include(item => item.PurchaseOrder)
            .SingleOrDefault(item => item.Id == purchaseReceivingId)
            ?? throw new InvalidOperationException("The purchase receiving record does not exist.");
        var order = receiving.PurchaseOrder;
        if (order.Status is not (PurchaseOrderStatus.Received or PurchaseOrderStatus.Closed))
            throw new InvalidOperationException("Only received purchase orders can be transferred to accounting.");
        if (!receiving.IsApproved || !receiving.IsFullReceiving || !receiving.QualityInspectionCompleted)
            throw new InvalidOperationException("Purchase receiving must be complete, quality-inspected, and approved before accounting transfer.");
        if (!receiving.SupplierInvoiceAmount.HasValue
            || Math.Abs(receiving.SupplierInvoiceAmount.Value - order.Total) > 0.01m)
            throw new InvalidOperationException("The approved supplier invoice must match the received purchase total.");
        return CreateOperationalDraft(
            PostingEventType.PurchaseReceived,
            nameof(PurchaseReceiving),
            receiving.Id.ToString(),
            receiving.ReceivingDate,
            fiscalPeriodId,
            $"Approved purchase receiving {receiving.ReceivingNumber}",
            receiving.SupplierInvoiceNumber ?? receiving.ReceivingNumber,
            receiving.SupplierInvoiceAmount.Value,
            order.VATAmount,
            actor,
            reason,
            PostingComponent.InventoryOrExpense,
            PostingComponent.AccountsPayable,
            PostingComponent.InputVat,
            debitPrimary: false,
            foreignCurrency);
    }

    public JournalEntry CreateCustomerPaymentDraft(int customerPaymentId, int fiscalPeriodId, string actor, string reason)
    {
        var payment = _context.CustomerPayments.AsNoTracking().SingleOrDefault(item => item.Id == customerPaymentId)
            ?? throw new InvalidOperationException("The customer payment does not exist.");
        if (!string.Equals(payment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only completed customer payments can be transferred to accounting.");
        EnsurePositiveSarAmount(payment.Amount);
        return CreateMappedDraft(
            PostingEventType.CustomerPaymentReceived, nameof(CustomerPayment), payment.Id.ToString(), payment.PaymentDate,
            fiscalPeriodId, $"Customer payment {payment.PaymentNumber}", payment.ReferenceNumber ?? payment.PaymentNumber,
            actor, reason,
            new[]
            {
                new ComponentPosting(PostingComponent.Cash, payment.Amount, 0m, "Cash received"),
                new ComponentPosting(PostingComponent.AccountsReceivable, 0m, payment.Amount, "Customer receivable settled")
            });
    }

    public JournalEntry CreateSupplierPaymentDraft(int supplierPaymentId, int fiscalPeriodId, string actor, string reason)
    {
        var payment = _context.SupplierPayments.AsNoTracking().SingleOrDefault(item => item.Id == supplierPaymentId)
            ?? throw new InvalidOperationException("The supplier payment does not exist.");
        if (!string.Equals(payment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only completed supplier payments can be transferred to accounting.");
        EnsurePositiveSarAmount(payment.Amount);
        return CreateMappedDraft(
            PostingEventType.SupplierPaymentCompleted, nameof(SupplierPayment), payment.Id.ToString(), payment.PaymentDate,
            fiscalPeriodId, $"Supplier payment {payment.PaymentNumber}", payment.ReferenceNumber ?? payment.PaymentNumber,
            actor, reason,
            new[]
            {
                new ComponentPosting(PostingComponent.AccountsPayable, payment.Amount, 0m, "Supplier payable settled"),
                new ComponentPosting(PostingComponent.Cash, 0m, payment.Amount, "Cash paid")
            });
    }

    public JournalEntry CreatePayrollAccrualDraft(int salaryId, int fiscalPeriodId, string actor, string reason)
    {
        var salary = _context.Salaries.AsNoTracking().SingleOrDefault(item => item.Id == salaryId)
            ?? throw new InvalidOperationException("The salary record does not exist.");
        if (salary.Status != SalaryStatus.Approved || string.IsNullOrWhiteSpace(salary.ApprovedBy))
            throw new InvalidOperationException("Only independently approved payroll can be transferred to accounting.");
        EnsurePositiveSarAmount(salary.GrossSalary);
        EnsurePositiveSarAmount(salary.NetSalary);
        var withholdings = salary.GrossSalary - salary.NetSalary;
        if (withholdings < 0 || Math.Abs(withholdings - salary.TotalDeductions) > 0.01m)
            throw new InvalidOperationException("Payroll gross, net, and deductions do not reconcile.");

        var postings = new List<ComponentPosting>
        {
            new(PostingComponent.PayrollExpense, salary.GrossSalary, 0m, "Gross payroll expense"),
            new(PostingComponent.SalariesPayable, 0m, salary.NetSalary, "Net salaries payable")
        };
        if (withholdings > 0)
            postings.Add(new ComponentPosting(PostingComponent.PayrollWithholdingsPayable, 0m, withholdings, "Payroll deductions payable"));
        return CreateMappedDraft(
            PostingEventType.PayrollApproved, nameof(Salary), salary.Id.ToString(), salary.PayPeriodEnd,
            fiscalPeriodId, $"Approved payroll {salary.SalaryNumber}", salary.SalaryNumber,
            actor, reason, postings);
    }

    public JournalEntry CreateDepreciationDraft(int depreciationId, int fiscalPeriodId, string actor, string reason)
    {
        var depreciation = _context.AssetDepreciations.AsNoTracking().SingleOrDefault(item => item.Id == depreciationId)
            ?? throw new InvalidOperationException("The depreciation record does not exist.");
        if (!depreciation.IsApproved || string.IsNullOrWhiteSpace(depreciation.ApprovedBy))
            throw new InvalidOperationException("Only approved depreciation can be transferred to accounting.");
        EnsurePositiveSarAmount(depreciation.DepreciationAmount);
        return CreateMappedDraft(
            PostingEventType.DepreciationApproved, nameof(AssetDepreciation), depreciation.Id.ToString(), depreciation.DepreciationDate,
            fiscalPeriodId, $"Approved depreciation {depreciation.PeriodName} / asset {depreciation.FixedAssetId}",
            $"DEP-{depreciation.FixedAssetId}-{depreciation.Year}{depreciation.Month:D2}", actor, reason,
            new[]
            {
                new ComponentPosting(PostingComponent.DepreciationExpense, depreciation.DepreciationAmount, 0m, "Depreciation expense"),
                new ComponentPosting(PostingComponent.AccumulatedDepreciation, 0m, depreciation.DepreciationAmount, "Accumulated depreciation")
            });
    }

    public JournalEntry CreateInventoryMovementDraft(int stockMovementId, int fiscalPeriodId, string actor, string reason)
    {
        var movement = _context.StockMovements.AsNoTracking().Include(item => item.InventoryItem)
            .SingleOrDefault(item => item.Id == stockMovementId)
            ?? throw new InvalidOperationException("The stock movement does not exist.");
        if (!movement.IsApproved || movement.IsRejected || movement.IsCancelled)
            throw new InvalidOperationException("Only an approved, active stock movement can be transferred to accounting.");
        if (movement.Quantity <= 0m)
            throw new InvalidOperationException("The stock movement quantity must be positive.");
        if (movement.MovementType == StockMovementType.Consumption
            && _context.ProductionCostEvents.Any(value => value.StockMovementId == movement.Id
                && value.EventType == ProductionCostEventType.InputConsumption))
            throw new InvalidOperationException(
                "Production consumption must be posted from its production cost event so it enters work in progress.");
        var amount = movement.TotalCost > 0m
            ? movement.TotalCost
            : movement.TotalAmount > 0m
                ? movement.TotalAmount
                : movement.Quantity * (movement.UnitCost > 0m ? movement.UnitCost : movement.InventoryItem.UnitCost);
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        EnsurePositiveSarAmount(amount);

        var postings = movement.MovementType switch
        {
            StockMovementType.Sale => InventoryDecrease(PostingComponent.CostOfGoodsSold, amount, "Cost of inventory sold"),
            StockMovementType.Consumption => InventoryDecrease(PostingComponent.InventoryConsumptionExpense, amount, "Inventory consumed in operations"),
            StockMovementType.Damage or StockMovementType.Expiry or StockMovementType.Expired
                or StockMovementType.Loss or StockMovementType.Waste or StockMovementType.AdjustmentDecrease =>
                InventoryDecrease(PostingComponent.InventoryLossExpense, amount, "Inventory loss or write-down"),
            StockMovementType.Found or StockMovementType.AdjustmentIncrease => new[]
            {
                new ComponentPosting(PostingComponent.InventoryAsset, amount, 0m, "Approved inventory increase"),
                new ComponentPosting(PostingComponent.InventoryAdjustmentGain, 0m, amount, "Inventory adjustment gain")
            },
            StockMovementType.Purchase => throw new InvalidOperationException(
                "Purchase stock is posted from the approved purchase receipt to prevent duplicate inventory recognition."),
            StockMovementType.Transfer => throw new InvalidOperationException(
                "A transfer within the same inventory ledger has no general-ledger entry."),
            _ => throw new InvalidOperationException(
                "This stock movement type requires a dedicated, unambiguous accounting workflow before transfer.")
        };
        var reference = movement.Reference ?? movement.ReferenceNumber ?? $"STOCK-{movement.Id}";
        return CreateMappedDraft(
            PostingEventType.InventoryMovementApproved, nameof(StockMovement), movement.Id.ToString(),
            movement.MovementDate, fiscalPeriodId,
            $"Approved {movement.MovementType} movement for {movement.InventoryItem.Name}", reference,
            actor, reason, postings);
    }

    public JournalEntry CreateProductionCostEventDraft(long productionCostEventId, int fiscalPeriodId,
        string actor, string reason)
    {
        var costEvent = _context.ProductionCostEvents.AsNoTracking()
            .SingleOrDefault(value => value.Id == productionCostEventId)
            ?? throw new InvalidOperationException("The production cost event does not exist.");
        if (costEvent.Amount <= 0m || decimal.Round(costEvent.Amount, 2) != costEvent.Amount)
            throw new InvalidOperationException("Only a positive valued production cost event requires a general-ledger entry.");
        var postings = costEvent.EventType switch
        {
            ProductionCostEventType.InputConsumption => new[]
            {
                new ComponentPosting(PostingComponent.WorkInProgressInventory, costEvent.Amount, 0m,
                    "Production input charged to work in progress"),
                new ComponentPosting(PostingComponent.InventoryAsset, 0m, costEvent.Amount,
                    "Input inventory issued to production")
            },
            ProductionCostEventType.DirectCost => new[]
            {
                new ComponentPosting(PostingComponent.WorkInProgressInventory, costEvent.Amount, 0m,
                    "Direct production cost charged to work in progress"),
                new ComponentPosting(PostingComponent.ProductionCostClearing, 0m, costEvent.Amount,
                    "Direct production cost clearing")
            },
            ProductionCostEventType.AbnormalMortality => new[]
            {
                new ComponentPosting(PostingComponent.InventoryLossExpense, costEvent.Amount, 0m,
                    "Abnormal mortality loss"),
                new ComponentPosting(PostingComponent.WorkInProgressInventory, 0m, costEvent.Amount,
                    "Abnormal mortality removed from work in progress")
            },
            ProductionCostEventType.HarvestCapitalization => new[]
            {
                new ComponentPosting(PostingComponent.InventoryAsset, costEvent.Amount, 0m,
                    "Harvest product capitalized in finished inventory"),
                new ComponentPosting(PostingComponent.WorkInProgressInventory, 0m, costEvent.Amount,
                    "Harvest cost released from work in progress")
            },
            ProductionCostEventType.NormalMortality or ProductionCostEventType.PondTransfer =>
                throw new InvalidOperationException("This production cost event does not change general-ledger account totals."),
            _ => throw new InvalidOperationException("The production cost event type has no approved posting rule.")
        };
        return CreateMappedDraft(
            PostingEventType.ProductionCostEventApproved, nameof(ProductionCostEvent), costEvent.Id.ToString(),
            costEvent.EventDate, fiscalPeriodId, $"Production cost: {costEvent.EventType} / {costEvent.Reference}",
            costEvent.Reference, actor, reason, postings);
    }

    public JournalEntry CreateVatReturnSettlementDraft(int vatReturnId, int fiscalPeriodId, string actor, string reason)
    {
        var vatReturn = _context.VATReturns.AsNoTracking().SingleOrDefault(item => item.Id == vatReturnId)
            ?? throw new InvalidOperationException("The VAT return does not exist.");
        if (vatReturn.Status is not (VATReturnStatus.Submitted or VATReturnStatus.Paid or VATReturnStatus.Closed)
            || !vatReturn.SubmissionDate.HasValue)
            throw new InvalidOperationException("Only a submitted VAT return with a submission date can be transferred to accounting.");
        if (vatReturn.Box12_Adjustments != 0m || vatReturn.Box14_RecoverablePreviousPeriod != 0m)
            throw new InvalidOperationException(
                "VAT return adjustments and prior-period recoverable balances require an accountant-approved adjustment workflow.");
        var reconciliation = _context.VatReturnLedgerReconciliations.AsNoTracking()
            .Where(item => item.VatReturnId == vatReturn.Id)
            .OrderByDescending(item => item.Version)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("A successful immutable VAT-to-ledger reconciliation is required before settlement.");
        if (!reconciliation.IsReconciled
            || reconciliation.PeriodStartDate.Date != vatReturn.PeriodStartDate.Date
            || reconciliation.PeriodEndDate.Date != vatReturn.PeriodEndDate.Date
            || reconciliation.DeclaredOutputVat != vatReturn.Box6_VATOnSales
            || reconciliation.DeclaredInputVat != vatReturn.Box10_VATOnPurchases)
            throw new InvalidOperationException("The VAT return no longer matches its approved ledger reconciliation.");
        var outputVat = vatReturn.Box6_VATOnSales;
        var inputVat = vatReturn.Box10_VATOnPurchases;
        if (outputVat < 0m || inputVat < 0m || decimal.Round(outputVat, 2) != outputVat || decimal.Round(inputVat, 2) != inputVat)
            throw new InvalidOperationException("VAT return amounts must be non-negative SAR values with two-decimal precision.");
        var net = outputVat - inputVat;
        if (outputVat == 0m && inputVat == 0m)
            throw new InvalidOperationException("The VAT return has no tax balances to settle.");
        if (vatReturn.Box15_NetVATDueForPeriod != net)
            throw new InvalidOperationException("The VAT return net amount does not reconcile to output VAT less input VAT.");

        var postings = new List<ComponentPosting>();
        if (outputVat > 0m) postings.Add(new ComponentPosting(PostingComponent.OutputVat, outputVat, 0m, "Clear output VAT"));
        if (inputVat > 0m) postings.Add(new ComponentPosting(PostingComponent.InputVat, 0m, inputVat, "Clear recoverable input VAT"));
        if (net > 0m) postings.Add(new ComponentPosting(PostingComponent.VatPayable, 0m, net, "Net VAT payable"));
        if (net < 0m) postings.Add(new ComponentPosting(PostingComponent.VatReceivable, -net, 0m, "Net VAT receivable"));

        return CreateMappedDraft(
            PostingEventType.VatReturnSubmitted, nameof(VATReturn), vatReturn.Id.ToString(),
            vatReturn.SubmissionDate.Value.Date, fiscalPeriodId,
            $"Submitted VAT return {vatReturn.PeriodNumber}", $"VAT-{vatReturn.PeriodNumber}",
            actor, reason, postings);
    }

    public JournalEntry CreateTaxAdjustmentNoteDraft(int taxInvoiceId, int fiscalPeriodId, string actor, string reason)
    {
        var note = _context.TaxInvoices.AsNoTracking().Include(item => item.OriginalTaxInvoice)
            .SingleOrDefault(item => item.Id == taxInvoiceId)
            ?? throw new InvalidOperationException("The tax adjustment document does not exist.");
        if (note.InvoiceType is not (InvoiceType.CreditNote or InvoiceType.DebitNote))
            throw new InvalidOperationException("Only a credit note or debit note can use the tax-adjustment posting workflow.");
        if (note.OriginalTaxInvoice == null
            || note.OriginalTaxInvoice.InvoiceType is InvoiceType.CreditNote or InvoiceType.DebitNote)
            throw new InvalidOperationException("The adjustment must reference an original standard or simplified tax invoice.");
        if (string.IsNullOrWhiteSpace(note.AdjustmentReason))
            throw new InvalidOperationException("A documented adjustment reason is required.");
        if (note.CustomerId != note.OriginalTaxInvoice.CustomerId
            || note.SellerVATNumber != note.OriginalTaxInvoice.SellerVATNumber)
            throw new InvalidOperationException("The adjustment customer and seller VAT number must match the original invoice.");
        var total = note.TotalWithVAT;
        var vat = note.VATAmount;
        if (total <= 0m || vat < 0m || vat > total
            || decimal.Round(total, 2) != total || decimal.Round(vat, 2) != vat)
            throw new InvalidOperationException("Tax adjustment amounts must be positive SAR values with two-decimal precision.");
        var net = total - vat;
        var isCredit = note.InvoiceType == InvoiceType.CreditNote;
        var postings = isCredit
            ? new[]
            {
                new ComponentPosting(PostingComponent.SalesRevenue, net, 0m, "Revenue reduced by credit note"),
                new ComponentPosting(PostingComponent.OutputVat, vat, 0m, "Output VAT reduced by credit note"),
                new ComponentPosting(PostingComponent.AccountsReceivable, 0m, total, "Receivable reduced by credit note")
            }
            : new[]
            {
                new ComponentPosting(PostingComponent.AccountsReceivable, total, 0m, "Receivable increased by debit note"),
                new ComponentPosting(PostingComponent.SalesRevenue, 0m, net, "Revenue increased by debit note"),
                new ComponentPosting(PostingComponent.OutputVat, 0m, vat, "Output VAT increased by debit note")
            };
        var eventType = isCredit ? PostingEventType.TaxCreditNoteIssued : PostingEventType.TaxDebitNoteIssued;
        return CreateMappedDraft(eventType, nameof(TaxInvoice), note.Id.ToString(), note.IssueDate,
            fiscalPeriodId, $"{note.InvoiceType} {note.InvoiceNumber} for {note.OriginalTaxInvoice.InvoiceNumber}",
            note.InvoiceNumber, actor, reason, postings);
    }

    private static ComponentPosting[] InventoryDecrease(PostingComponent expenseComponent, decimal amount, string description) =>
    [
        new ComponentPosting(expenseComponent, amount, 0m, description),
        new ComponentPosting(PostingComponent.InventoryAsset, 0m, amount, "Inventory asset decrease")
    ];

    private JournalEntry CreateOperationalDraft(
        PostingEventType eventType,
        string sourceEntityType,
        string sourceEntityId,
        DateTime entryDate,
        int fiscalPeriodId,
        string description,
        string reference,
        decimal total,
        decimal vat,
        string actor,
        string reason,
        PostingComponent primaryDebitOrCredit,
        PostingComponent oppositeDebitOrCredit,
        PostingComponent vatComponent,
        bool debitPrimary,
        OperationalForeignCurrencyMeasurement? foreignCurrency = null)
    {
        if (total <= 0 || vat < 0 || vat > total)
            throw new InvalidOperationException("Operational totals are invalid for accounting transfer.");
        var net = total - vat;
        if (decimal.Round(total, 2) != total || decimal.Round(vat, 2) != vat || net <= 0)
            throw new InvalidOperationException("Operational totals must be positive SAR amounts with two-decimal precision.");
        var postings = new List<ComponentPosting>();
        if (debitPrimary)
        {
            postings.Add(new ComponentPosting(primaryDebitOrCredit, total, 0m, "Gross receivable"));
            postings.Add(new ComponentPosting(oppositeDebitOrCredit, 0m, net, "Net revenue"));
            if (vat > 0)
                postings.Add(new ComponentPosting(vatComponent, 0m, vat, "Output VAT"));
        }
        else
        {
            postings.Add(new ComponentPosting(primaryDebitOrCredit, net, 0m, "Net inventory or expense"));
            if (vat > 0)
                postings.Add(new ComponentPosting(vatComponent, vat, 0m, "Recoverable input VAT"));
            postings.Add(new ComponentPosting(oppositeDebitOrCredit, 0m, total, "Supplier payable"));
        }

        return CreateMappedDraft(eventType, sourceEntityType, sourceEntityId, entryDate, fiscalPeriodId,
            description, reference, actor, reason, postings, foreignCurrency,
            debitPrimary ? primaryDebitOrCredit : oppositeDebitOrCredit, total);
    }

    private JournalEntry CreateMappedDraft(
        PostingEventType eventType,
        string sourceEntityType,
        string sourceEntityId,
        DateTime entryDate,
        int fiscalPeriodId,
        string description,
        string reference,
        string actor,
        string reason,
        IReadOnlyCollection<ComponentPosting> postings,
        OperationalForeignCurrencyMeasurement? foreignCurrency = null,
        PostingComponent? foreignMonetaryComponent = null,
        decimal? foreignMonetarySarAmount = null)
    {
        if (_context.OperationalPostingRecords.Any(item => item.EventType == eventType
            && item.SourceEntityType == sourceEntityType && item.SourceEntityId == sourceEntityId))
            throw new InvalidOperationException("This operational event has already been transferred to accounting.");
        var configuration = _context.AccountingConfigurations.AsNoTracking()
            .Where(item => item.Status == AccountingConfigurationStatus.Approved)
            .OrderByDescending(item => item.Version)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No approved accounting configuration is available.");
        var mappings = _context.PostingMappings.AsNoTracking()
            .Where(item => item.AccountingConfigurationId == configuration.Id
                && item.EventType == eventType && item.IsActive)
            .ToDictionary(item => item.Component, item => item.LedgerAccountId);
        foreach (var component in postings.Select(item => item.Component).Distinct())
        {
            if (!mappings.ContainsKey(component))
                throw new InvalidOperationException($"The approved posting map is missing {component}.");
        }
        var foreignMeasurement = ValidateForeignCurrencyMeasurement(
            foreignCurrency, entryDate, foreignMonetarySarAmount);
        var lines = postings.Select(item => item.Component == foreignMonetaryComponent && foreignMeasurement != null
            ? new JournalLineRequest(mappings[item.Component], item.Debit, item.Credit,
                Description: item.Description,
                ForeignCurrencyCode: foreignMeasurement.Value.CurrencyCode,
                ForeignAmount: foreignMeasurement.Value.ForeignAmount,
                ForeignExchangeRateId: foreignMeasurement.Value.ExchangeRateId)
            : new JournalLineRequest(mappings[item.Component], item.Debit, item.Credit,
                Description: item.Description)).ToArray();

        using var transaction = _context.Database.CurrentTransaction == null
            ? _context.Database.BeginTransaction(IsolationLevel.Serializable)
            : null;
        var entry = _ledger.CreateDraft(new JournalDraftRequest(
            entryDate.Date, fiscalPeriodId, description, eventType.ToString(), reference, lines), actor, reason);
        _context.OperationalPostingRecords.Add(new OperationalPostingRecord
        {
            EventType = eventType,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            JournalEntryId = entry.Id,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actor.Trim()
        });
        _context.SaveChanges();
        transaction?.Commit();
        return entry;
    }

    private (string CurrencyCode, decimal ForeignAmount, long ExchangeRateId)? ValidateForeignCurrencyMeasurement(
        OperationalForeignCurrencyMeasurement? measurement, DateTime entryDate, decimal? sarAmount)
    {
        if (measurement == null) return null;
        if (!sarAmount.HasValue || sarAmount <= 0m || measurement.ForeignGrossAmount <= 0m)
            throw new InvalidOperationException("A positive foreign gross amount and SAR monetary amount are required.");
        var currency = measurement.CurrencyCode.Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency == "SAR")
            throw new InvalidOperationException("The operational foreign currency must be a three-letter code other than SAR.");
        var rate = _context.ForeignExchangeRates.AsNoTracking().SingleOrDefault(value =>
            value.Id == measurement.ExchangeRateId)
            ?? throw new InvalidOperationException("The selected exchange rate does not exist.");
        if (rate.Status != ExchangeRateStatus.Approved || rate.Purpose != ExchangeRatePurpose.Transaction
            || rate.RateDate.Date != entryDate.Date || rate.CurrencyCode != currency)
            throw new InvalidOperationException("The transaction requires an approved same-date rate for the same currency.");
        var translated = decimal.Round(measurement.ForeignGrossAmount * rate.SarPerUnit, 2,
            MidpointRounding.AwayFromZero);
        if (translated != sarAmount.Value)
            throw new InvalidOperationException("The foreign gross amount translated at the approved rate must equal the SAR receivable or payable.");
        return (currency, measurement.ForeignGrossAmount, rate.Id);
    }

    private static void EnsurePositiveSarAmount(decimal amount)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException("The operational amount must be a positive SAR value with two-decimal precision.");
    }
}
