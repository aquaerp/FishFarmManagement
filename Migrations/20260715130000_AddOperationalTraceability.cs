using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715130000_AddOperationalTraceability")]
public sealed class AddOperationalTraceability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "TraceabilityLots",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                LotCode = table.Column<string>("TEXT", maxLength: 120, nullable: false),
                Kind = table.Column<int>("INTEGER", nullable: false),
                InitialQuantity = table.Column<decimal>("TEXT", precision: 18, scale: 3, nullable: false),
                Unit = table.Column<string>("TEXT", maxLength: 50, nullable: false),
                LotDate = table.Column<DateTime>("TEXT", nullable: false),
                SourceReference = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                InventoryItemId = table.Column<int>("INTEGER", nullable: true),
                PurchaseReceivingItemId = table.Column<int>("INTEGER", nullable: true),
                ProductionCycleId = table.Column<int>("INTEGER", nullable: true),
                PondId = table.Column<int>("INTEGER", nullable: true),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TraceabilityLots", x => x.Id);
                table.CheckConstraint("CK_TraceabilityLot_PositiveQuantity", "CAST(InitialQuantity AS NUMERIC) > 0");
                table.CheckConstraint("CK_TraceabilityLot_KindShape",
                    "(Kind = 0 AND InventoryItemId IS NOT NULL AND PurchaseReceivingItemId IS NOT NULL AND ProductionCycleId IS NULL AND PondId IS NULL) OR " +
                    "(Kind = 1 AND InventoryItemId IS NULL AND PurchaseReceivingItemId IS NULL AND ProductionCycleId IS NOT NULL AND PondId IS NOT NULL)");
                table.ForeignKey("FK_TraceabilityLots_InventoryItems_InventoryItemId", x => x.InventoryItemId,
                    "InventoryItems", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityLots_PurchaseReceivingItems_PurchaseReceivingItemId", x => x.PurchaseReceivingItemId,
                    "PurchaseReceivingItems", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityLots_ProductionCycles_ProductionCycleId", x => x.ProductionCycleId,
                    "ProductionCycles", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityLots_Ponds_PondId", x => x.PondId,
                    "Ponds", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_TraceabilityLots_LotCode", "TraceabilityLots", "LotCode", unique: true);
        migrationBuilder.CreateIndex("IX_TraceabilityLots_InventoryItemId", "TraceabilityLots", "InventoryItemId");
        migrationBuilder.CreateIndex("IX_TraceabilityLots_PurchaseReceivingItemId", "TraceabilityLots", "PurchaseReceivingItemId", unique: true);
        migrationBuilder.CreateIndex("IX_TraceabilityLots_ProductionCycleId", "TraceabilityLots", "ProductionCycleId");
        migrationBuilder.CreateIndex("IX_TraceabilityLots_PondId", "TraceabilityLots", "PondId");

        migrationBuilder.CreateTable(
            "TraceabilityAllocations",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                AllocationType = table.Column<int>("INTEGER", nullable: false),
                SourceLotId = table.Column<long>("INTEGER", nullable: false),
                Quantity = table.Column<decimal>("TEXT", precision: 18, scale: 3, nullable: false),
                EventDate = table.Column<DateTime>("TEXT", nullable: false),
                Reference = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                ProductionCycleId = table.Column<int>("INTEGER", nullable: true),
                PondId = table.Column<int>("INTEGER", nullable: true),
                StockMovementId = table.Column<int>("INTEGER", nullable: true),
                SalesOrderItemId = table.Column<int>("INTEGER", nullable: true),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TraceabilityAllocations", x => x.Id);
                table.CheckConstraint("CK_TraceabilityAllocation_PositiveQuantity", "CAST(Quantity AS NUMERIC) > 0");
                table.CheckConstraint("CK_TraceabilityAllocation_TypeShape",
                    "(AllocationType = 0 AND ProductionCycleId IS NOT NULL AND PondId IS NOT NULL AND StockMovementId IS NOT NULL AND SalesOrderItemId IS NULL) OR " +
                    "(AllocationType = 1 AND ProductionCycleId IS NULL AND PondId IS NULL AND StockMovementId IS NULL AND SalesOrderItemId IS NOT NULL)");
                table.ForeignKey("FK_TraceabilityAllocations_TraceabilityLots_SourceLotId", x => x.SourceLotId,
                    "TraceabilityLots", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityAllocations_ProductionCycles_ProductionCycleId", x => x.ProductionCycleId,
                    "ProductionCycles", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityAllocations_Ponds_PondId", x => x.PondId,
                    "Ponds", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityAllocations_StockMovements_StockMovementId", x => x.StockMovementId,
                    "StockMovements", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TraceabilityAllocations_SalesOrderItems_SalesOrderItemId", x => x.SalesOrderItemId,
                    "SalesOrderItems", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_TraceabilityAllocations_AllocationType_SourceLotId_Reference", "TraceabilityAllocations",
            new[] { "AllocationType", "SourceLotId", "Reference" }, unique: true);
        migrationBuilder.CreateIndex("IX_TraceabilityAllocations_SourceLotId", "TraceabilityAllocations", "SourceLotId");
        migrationBuilder.CreateIndex("IX_TraceabilityAllocations_ProductionCycleId", "TraceabilityAllocations", "ProductionCycleId");
        migrationBuilder.CreateIndex("IX_TraceabilityAllocations_PondId", "TraceabilityAllocations", "PondId");
        migrationBuilder.CreateIndex("IX_TraceabilityAllocations_StockMovementId", "TraceabilityAllocations", "StockMovementId");
        migrationBuilder.CreateIndex("IX_TraceabilityAllocations_SalesOrderItemId", "TraceabilityAllocations", "SalesOrderItemId");

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TraceabilityLots_Immutable
            BEFORE UPDATE ON TraceabilityLots BEGIN SELECT RAISE(ABORT, 'Traceability lots are immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TraceabilityLots_NoDelete
            BEFORE DELETE ON TraceabilityLots BEGIN SELECT RAISE(ABORT, 'Traceability lots cannot be deleted'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TraceabilityAllocations_Immutable
            BEFORE UPDATE ON TraceabilityAllocations BEGIN SELECT RAISE(ABORT, 'Traceability allocations are immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TraceabilityAllocations_NoDelete
            BEFORE DELETE ON TraceabilityAllocations BEGIN SELECT RAISE(ABORT, 'Traceability allocations cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TraceabilityAllocations_Immutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TraceabilityAllocations_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TraceabilityLots_Immutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TraceabilityLots_NoDelete;");
        migrationBuilder.DropTable("TraceabilityAllocations");
        migrationBuilder.DropTable("TraceabilityLots");
    }
}
