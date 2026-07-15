using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715120000_AddGovernedInventoryCounts")]
public sealed class AddGovernedInventoryCounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "InventoryCounts",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                CountNumber = table.Column<string>("TEXT", maxLength: 40, nullable: false),
                CountType = table.Column<int>("INTEGER", nullable: false),
                Status = table.Column<int>("INTEGER", nullable: false),
                CountDate = table.Column<DateTime>("TEXT", nullable: false),
                Reference = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Notes = table.Column<string>("TEXT", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                SubmittedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ApprovedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ApprovedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                ApprovalReason = table.Column<string>("TEXT", maxLength: 500, nullable: true),
                RejectedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                RejectedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                RejectionReason = table.Column<string>("TEXT", maxLength: 500, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_InventoryCounts", x => x.Id));
        migrationBuilder.CreateIndex("IX_InventoryCounts_CountNumber", "InventoryCounts", "CountNumber", unique: true);

        migrationBuilder.CreateTable(
            "InventoryCountLines",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                InventoryCountId = table.Column<long>("INTEGER", nullable: false),
                InventoryItemId = table.Column<int>("INTEGER", nullable: false),
                BookQuantitySnapshot = table.Column<decimal>("TEXT", precision: 18, scale: 3, nullable: false),
                UnitCostSnapshot = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                ActualQuantity = table.Column<decimal>("TEXT", precision: 18, scale: 3, nullable: true),
                VarianceQuantity = table.Column<decimal>("TEXT", precision: 18, scale: 3, nullable: false),
                VarianceValue = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                VarianceReason = table.Column<string>("TEXT", maxLength: 500, nullable: true),
                StockMovementId = table.Column<int>("INTEGER", nullable: true),
                JournalEntryId = table.Column<long>("INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryCountLines", x => x.Id);
                table.CheckConstraint("CK_InventoryCountLine_BookQuantity", "CAST(BookQuantitySnapshot AS NUMERIC) >= 0");
                table.CheckConstraint("CK_InventoryCountLine_ActualQuantity", "ActualQuantity IS NULL OR CAST(ActualQuantity AS NUMERIC) >= 0");
                table.ForeignKey("FK_InventoryCountLines_InventoryCounts_InventoryCountId", x => x.InventoryCountId,
                    "InventoryCounts", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_InventoryCountLines_InventoryItems_InventoryItemId", x => x.InventoryItemId,
                    "InventoryItems", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_InventoryCountLines_StockMovements_StockMovementId", x => x.StockMovementId,
                    "StockMovements", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_InventoryCountLines_JournalEntries_JournalEntryId", x => x.JournalEntryId,
                    "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_InventoryCountLines_InventoryCountId_InventoryItemId", "InventoryCountLines",
            new[] { "InventoryCountId", "InventoryItemId" }, unique: true);
        migrationBuilder.CreateIndex("IX_InventoryCountLines_InventoryItemId", "InventoryCountLines", "InventoryItemId");
        migrationBuilder.CreateIndex("IX_InventoryCountLines_StockMovementId", "InventoryCountLines", "StockMovementId", unique: true);
        migrationBuilder.CreateIndex("IX_InventoryCountLines_JournalEntryId", "InventoryCountLines", "JournalEntryId", unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryCounts_FinalImmutable
            BEFORE UPDATE ON InventoryCounts WHEN OLD.Status IN (2, 3)
            BEGIN SELECT RAISE(ABORT, 'Final inventory counts are immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryCounts_FinalNoDelete
            BEFORE DELETE ON InventoryCounts WHEN OLD.Status IN (1, 2, 3)
            BEGIN SELECT RAISE(ABORT, 'Submitted or final inventory counts cannot be deleted'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryCountLines_SubmittedImmutable
            BEFORE UPDATE ON InventoryCountLines
            WHEN (SELECT Status FROM InventoryCounts WHERE Id = OLD.InventoryCountId) IN (2, 3)
                OR ((SELECT Status FROM InventoryCounts WHERE Id = OLD.InventoryCountId) = 1
                AND (NEW.InventoryCountId IS NOT OLD.InventoryCountId
                    OR NEW.InventoryItemId IS NOT OLD.InventoryItemId
                    OR NEW.BookQuantitySnapshot IS NOT OLD.BookQuantitySnapshot
                    OR NEW.UnitCostSnapshot IS NOT OLD.UnitCostSnapshot
                    OR NEW.ActualQuantity IS NOT OLD.ActualQuantity
                    OR NEW.VarianceQuantity IS NOT OLD.VarianceQuantity
                    OR NEW.VarianceValue IS NOT OLD.VarianceValue
                    OR NEW.VarianceReason IS NOT OLD.VarianceReason))
            BEGIN SELECT RAISE(ABORT, 'Submitted inventory count lines are immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryCountLines_SubmittedNoDelete
            BEFORE DELETE ON InventoryCountLines
            WHEN (SELECT Status FROM InventoryCounts WHERE Id = OLD.InventoryCountId) IN (1, 2, 3)
            BEGIN SELECT RAISE(ABORT, 'Submitted inventory count lines cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryCounts_FinalImmutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryCounts_FinalNoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryCountLines_SubmittedImmutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryCountLines_SubmittedNoDelete;");
        migrationBuilder.DropTable("InventoryCountLines");
        migrationBuilder.DropTable("InventoryCounts");
    }
}
