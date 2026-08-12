using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class SalesOrderDocumentServiceTests
{
    [Fact]
    public void BuildInvoiceLines_UsesPersistedCustomerItemsAndTotals()
    {
        var order = new SalesOrder
        {
            OrderNumber = "SO-1", Customer = new Customer { Name = "عميل" }, SubTotal = 100m,
            VATAmount = 15m, GrandTotal = 115m,
            Items = new List<SalesOrderItem> { new() { ProductName = "سمك", Quantity = 5m, UnitPrice = 20m, TotalPrice = 100m } }
        };
        var text = string.Join('\n', new SalesOrderDocumentService().BuildInvoiceLines(order));
        Assert.Contains("SO-1", text);
        Assert.Contains("عميل", text);
        Assert.Contains("سمك", text);
        Assert.Contains("115", text);
    }
}
