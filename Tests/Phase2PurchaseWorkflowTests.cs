using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Phase2PurchaseWorkflowTests
{
    [Fact]
    public async Task PurchaseOrderDraft_PersistsItemsTotalsAndRemainingQuantities()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"aquafarm-purchase-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var context = new FishFarmContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                var supplier = new Supplier
                {
                    Name = "Phase 2 Supplier",
                    Status = SupplierStatus.Active,
                    Type = SupplierType.Feed,
                    CreatedAt = DateTime.UtcNow
                };
                var item = new InventoryItem
                {
                    Name = "Phase 2 Feed",
                    ItemName = "Phase 2 Feed",
                    Code = "P2-FEED",
                    ItemCode = "P2-FEED",
                    Category = InventoryCategory.Feed,
                    Unit = "kg",
                    UnitOfMeasure = "kg",
                    CurrentStock = 0,
                    MinimumStock = 10,
                    MaximumStock = 100,
                    ReorderPoint = 20,
                    UnitCost = 5,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.AddRange(supplier, item);
                await context.SaveChangesAsync();

                var order = new PurchaseOrder
                {
                    OrderNumber = "PO-PHASE2-001",
                    OrderDate = DateTime.UtcNow.Date,
                    ExpectedDeliveryDate = DateTime.UtcNow.Date.AddDays(7),
                    SupplierId = supplier.Id,
                    Status = PurchaseOrderStatus.Draft,
                    VATRate = 0.15m,
                    CreatedBy = "phase2-test"
                };
                var orderItem = new PurchaseOrderItem
                {
                    InventoryItemId = item.Id,
                    ItemName = item.Name,
                    Quantity = 10,
                    RemainingQuantity = 10,
                    UnitPrice = 5,
                    TotalPrice = 50,
                    CreatedAt = DateTime.UtcNow
                };
                order.Items.Add(orderItem);
                order.CalculateTotals();
                context.PurchaseOrders.Add(order);
                await context.SaveChangesAsync();
            }

            await using (var verificationContext = new FishFarmContext(options))
            {
                var saved = await verificationContext.PurchaseOrders
                    .Include(value => value.Items)
                    .SingleAsync(value => value.OrderNumber == "PO-PHASE2-001");
                Assert.Equal(PurchaseOrderStatus.Draft, saved.Status);
                Assert.Equal(57.50m, saved.Total);
                Assert.Single(saved.Items);
                Assert.Equal(10m, saved.Items.Single().RemainingQuantity);
            }
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }
}
