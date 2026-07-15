using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715110000_AddInventoryTransactionGuards")]
public sealed class AddInventoryTransactionGuards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ApprovedByUsername", "StockMovements", "TEXT", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>("ApprovalReason", "StockMovements", "TEXT", maxLength: 500, nullable: true);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryItems_NoNegativeInsert
            BEFORE INSERT ON InventoryItems WHEN CAST(NEW.CurrentStock AS NUMERIC) < 0
            BEGIN SELECT RAISE(ABORT, 'Negative inventory is not permitted'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_InventoryItems_NoNegativeUpdate
            BEFORE UPDATE OF CurrentStock ON InventoryItems WHEN CAST(NEW.CurrentStock AS NUMERIC) < 0
            BEGIN SELECT RAISE(ABORT, 'Negative inventory is not permitted'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_StockMovements_ApprovedImmutable
            BEFORE UPDATE ON StockMovements WHEN OLD.IsApproved = 1
            BEGIN SELECT RAISE(ABORT, 'Approved stock movements are immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_StockMovements_ApprovedNoDelete
            BEFORE DELETE ON StockMovements WHEN OLD.IsApproved = 1
            BEGIN SELECT RAISE(ABORT, 'Approved stock movements cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryItems_NoNegativeInsert;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_InventoryItems_NoNegativeUpdate;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_StockMovements_ApprovedImmutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_StockMovements_ApprovedNoDelete;");
        migrationBuilder.DropColumn("ApprovedByUsername", "StockMovements");
        migrationBuilder.DropColumn("ApprovalReason", "StockMovements");
    }
}
