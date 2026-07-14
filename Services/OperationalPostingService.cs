using System.Data;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

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

    public JournalEntry CreateSalesCompletionDraft(int salesOrderId, int fiscalPeriodId, string actor, string reason)
    {
        var order = _context.SalesOrders.AsNoTracking().SingleOrDefault(item => item.Id == salesOrderId)
            ?? throw new InvalidOperationException("The sales order does not exist.");
        if (order.Status != SalesOrderStatus.Completed)
            throw new InvalidOperationException("Only completed sales orders can be transferred to accounting.");
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
            debitPrimary: true);
    }

    public JournalEntry CreatePurchaseReceiptDraft(int purchaseReceivingId, int fiscalPeriodId, string actor, string reason)
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
            debitPrimary: false);
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
        bool debitPrimary)
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
            description, reference, actor, reason, postings);
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
        IReadOnlyCollection<ComponentPosting> postings)
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
        var lines = postings.Select(item => new JournalLineRequest(
            mappings[item.Component], item.Debit, item.Credit, Description: item.Description)).ToArray();

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
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
        transaction.Commit();
        return entry;
    }

    private static void EnsurePositiveSarAmount(decimal amount)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException("The operational amount must be a positive SAR value with two-decimal precision.");
    }
}
