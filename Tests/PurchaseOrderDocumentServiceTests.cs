using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class PurchaseOrderDocumentServiceTests
{
    [Fact]
    public void BuildLines_IncludesSupplierItemsAndRecordedTotals()
    {
        var order = new PurchaseOrder
        {
            OrderNumber = "PO-100",
            OrderDate = new DateTime(2026, 8, 12),
            ExpectedDeliveryDate = new DateTime(2026, 8, 20),
            Supplier = new Supplier { Name = "مورد الأعلاف" },
            SubTotal = 200m,
            VATAmount = 30m,
            Total = 230m,
            Items = new List<PurchaseOrderItem>
            {
                new() { Id = 1, ItemName = "علف", Quantity = 10m, UnitPrice = 20m, TotalPrice = 200m }
            }
        };

        var text = string.Join('\n', new PurchaseOrderDocumentService().BuildLines(order));

        Assert.Contains("PO-100", text);
        Assert.Contains("مورد الأعلاف", text);
        Assert.Contains("علف", text);
        Assert.Contains("230", text);
    }

    [Fact]
    public void BuildLines_RejectsAnOrderWithoutItems()
    {
        var order = new PurchaseOrder
        {
            OrderNumber = "PO-EMPTY",
            Supplier = new Supplier { Name = "Supplier" }
        };

        Assert.Throws<InvalidOperationException>(() => new PurchaseOrderDocumentService().BuildLines(order));
    }
}
