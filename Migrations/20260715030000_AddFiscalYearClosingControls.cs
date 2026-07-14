using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715030000_AddFiscalYearClosingControls")]
public sealed class AddFiscalYearClosingControls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("ClosedAtUtc", "FiscalYears", "TEXT", nullable: true);
        migrationBuilder.AddColumn<string>("ClosedBy", "FiscalYears", "TEXT", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<long>("OpeningBalanceJournalEntryId", "FiscalYears", "INTEGER", nullable: true);
        migrationBuilder.AddColumn<long>("ClosingJournalEntryId", "FiscalYears", "INTEGER", nullable: true);

        migrationBuilder.CreateIndex(
            "IX_FiscalYears_OpeningBalanceJournalEntryId",
            "FiscalYears",
            "OpeningBalanceJournalEntryId",
            unique: true);
        migrationBuilder.CreateIndex(
            "IX_FiscalYears_ClosingJournalEntryId",
            "FiscalYears",
            "ClosingJournalEntryId",
            unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_FiscalYears_ClosedImmutable
            BEFORE UPDATE ON FiscalYears
            WHEN OLD.IsClosed = 1 AND (
                NEW.Name <> OLD.Name OR
                NEW.StartDate <> OLD.StartDate OR
                NEW.EndDate <> OLD.EndDate OR
                NEW.IsClosed <> OLD.IsClosed OR
                NEW.ClosedAtUtc IS NOT OLD.ClosedAtUtc OR
                NEW.ClosedBy IS NOT OLD.ClosedBy OR
                NEW.OpeningBalanceJournalEntryId IS NOT OLD.OpeningBalanceJournalEntryId OR
                NEW.ClosingJournalEntryId IS NOT OLD.ClosingJournalEntryId
            )
            BEGIN
                SELECT RAISE(ABORT, 'Closed fiscal years are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_FiscalYears_ClosedNoDelete
            BEFORE DELETE ON FiscalYears
            WHEN OLD.IsClosed = 1
            BEGIN
                SELECT RAISE(ABORT, 'Closed fiscal years cannot be deleted');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_FiscalYears_ClosedImmutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_FiscalYears_ClosedNoDelete;");
        migrationBuilder.DropIndex("IX_FiscalYears_OpeningBalanceJournalEntryId", "FiscalYears");
        migrationBuilder.DropIndex("IX_FiscalYears_ClosingJournalEntryId", "FiscalYears");
        migrationBuilder.DropColumn("ClosedAtUtc", "FiscalYears");
        migrationBuilder.DropColumn("ClosedBy", "FiscalYears");
        migrationBuilder.DropColumn("OpeningBalanceJournalEntryId", "FiscalYears");
        migrationBuilder.DropColumn("ClosingJournalEntryId", "FiscalYears");
    }
}
