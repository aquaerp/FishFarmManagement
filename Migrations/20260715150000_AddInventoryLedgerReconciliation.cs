using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715150000_AddInventoryLedgerReconciliation")]
public sealed class AddInventoryLedgerReconciliation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "InventoryLedgerReconciliations",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                AsOfDate = table.Column<DateTime>("TEXT", nullable: false),
                InventoryItemCount = table.Column<int>("INTEGER", nullable: false),
                QuantityExceptionCount = table.Column<int>("INTEGER", nullable: false),
                ValuationExceptionCount = table.Column<int>("INTEGER", nullable: false),
                InventorySubledgerValue = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                InventoryGeneralLedgerValue = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                InventoryDifference = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                WorkInProgressSubledgerValue = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                WorkInProgressGeneralLedgerValue = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                WorkInProgressDifference = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                IsPassed = table.Column<bool>("INTEGER", nullable: false),
                EvidenceJson = table.Column<string>("TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Reason = table.Column<string>("TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryLedgerReconciliations", x => x.Id);
                table.CheckConstraint("CK_InventoryLedgerReconciliation_Counts",
                    "InventoryItemCount >= 0 AND QuantityExceptionCount >= 0 AND ValuationExceptionCount >= 0");
            });
        migrationBuilder.CreateIndex("IX_InventoryLedgerReconciliations_AsOfDate",
            "InventoryLedgerReconciliations", "AsOfDate");
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryLedgerReconciliations_Immutable
            BEFORE UPDATE ON InventoryLedgerReconciliations BEGIN SELECT RAISE(ABORT, 'Inventory reconciliation evidence is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryLedgerReconciliations_NoDelete
            BEFORE DELETE ON InventoryLedgerReconciliations BEGIN SELECT RAISE(ABORT, 'Inventory reconciliation evidence cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryLedgerReconciliations_Immutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryLedgerReconciliations_NoDelete;");
        migrationBuilder.DropTable("InventoryLedgerReconciliations");
    }
}
