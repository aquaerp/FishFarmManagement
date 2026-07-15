using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715140000_AddProductionCostLedger")]
public sealed class AddProductionCostLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ProductionCostEvents",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                EventType = table.Column<int>("INTEGER", nullable: false),
                ProductionCycleId = table.Column<int>("INTEGER", nullable: false),
                PondId = table.Column<int>("INTEGER", nullable: false),
                DestinationPondId = table.Column<int>("INTEGER", nullable: true),
                QuantityKg = table.Column<decimal>("TEXT", precision: 18, scale: 3, nullable: false),
                Amount = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                Reference = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                SourceEntityType = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                SourceEntityId = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                StockMovementId = table.Column<int>("INTEGER", nullable: true),
                MortalityRecordId = table.Column<int>("INTEGER", nullable: true),
                CostRecordId = table.Column<int>("INTEGER", nullable: true),
                HarvestLotId = table.Column<long>("INTEGER", nullable: true),
                EventDate = table.Column<DateTime>("TEXT", nullable: false),
                MeasurementBasis = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductionCostEvents", x => x.Id);
                table.CheckConstraint("CK_ProductionCostEvent_NonNegative",
                    "CAST(QuantityKg AS NUMERIC) >= 0 AND CAST(Amount AS NUMERIC) >= 0");
                table.CheckConstraint("CK_ProductionCostEvent_TransferShape",
                    "(EventType = 4 AND DestinationPondId IS NOT NULL AND DestinationPondId <> PondId AND CAST(QuantityKg AS NUMERIC) > 0 AND CAST(Amount AS NUMERIC) > 0) OR " +
                    "(EventType <> 4 AND DestinationPondId IS NULL)");
                table.ForeignKey("FK_ProductionCostEvents_ProductionCycles_ProductionCycleId", x => x.ProductionCycleId,
                    "ProductionCycles", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProductionCostEvents_Ponds_PondId", x => x.PondId,
                    "Ponds", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProductionCostEvents_Ponds_DestinationPondId", x => x.DestinationPondId,
                    "Ponds", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProductionCostEvents_StockMovements_StockMovementId", x => x.StockMovementId,
                    "StockMovements", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProductionCostEvents_MortalityRecords_MortalityRecordId", x => x.MortalityRecordId,
                    "MortalityRecords", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProductionCostEvents_CostRecords_CostRecordId", x => x.CostRecordId,
                    "CostRecords", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ProductionCostEvents_TraceabilityLots_HarvestLotId", x => x.HarvestLotId,
                    "TraceabilityLots", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_EventType_SourceEntityType_SourceEntityId",
            "ProductionCostEvents", new[] { "EventType", "SourceEntityType", "SourceEntityId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_ProductionCycleId", "ProductionCostEvents", "ProductionCycleId");
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_PondId", "ProductionCostEvents", "PondId");
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_DestinationPondId", "ProductionCostEvents", "DestinationPondId");
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_StockMovementId", "ProductionCostEvents", "StockMovementId");
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_MortalityRecordId", "ProductionCostEvents", "MortalityRecordId");
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_CostRecordId", "ProductionCostEvents", "CostRecordId");
        migrationBuilder.CreateIndex("IX_ProductionCostEvents_HarvestLotId", "ProductionCostEvents", "HarvestLotId");
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ProductionCostEvents_Immutable
            BEFORE UPDATE ON ProductionCostEvents BEGIN SELECT RAISE(ABORT, 'Production cost events are immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ProductionCostEvents_NoDelete
            BEFORE DELETE ON ProductionCostEvents BEGIN SELECT RAISE(ABORT, 'Production cost events cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ProductionCostEvents_Immutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ProductionCostEvents_NoDelete;");
        migrationBuilder.DropTable("ProductionCostEvents");
    }
}
