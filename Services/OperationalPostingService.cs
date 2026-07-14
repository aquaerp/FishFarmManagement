using System.Data;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed class OperationalPostingService
{
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
        foreach (var component in new[] { primaryDebitOrCredit, oppositeDebitOrCredit, vatComponent })
        {
            if (!mappings.ContainsKey(component))
                throw new InvalidOperationException($"The approved posting map is missing {component}.");
        }

        var lines = new List<JournalLineRequest>();
        if (debitPrimary)
        {
            lines.Add(new JournalLineRequest(mappings[primaryDebitOrCredit], total, 0m, Description: "Gross receivable"));
            lines.Add(new JournalLineRequest(mappings[oppositeDebitOrCredit], 0m, net, Description: "Net revenue"));
            if (vat > 0)
                lines.Add(new JournalLineRequest(mappings[vatComponent], 0m, vat, Description: "Output VAT"));
        }
        else
        {
            lines.Add(new JournalLineRequest(mappings[primaryDebitOrCredit], net, 0m, Description: "Net inventory or expense"));
            if (vat > 0)
                lines.Add(new JournalLineRequest(mappings[vatComponent], vat, 0m, Description: "Recoverable input VAT"));
            lines.Add(new JournalLineRequest(mappings[oppositeDebitOrCredit], 0m, total, Description: "Supplier payable"));
        }

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
}
