using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715060000_AddAccountingAdjustments")]
public sealed class AddAccountingAdjustments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "AccountingAdjustments",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                JournalEntryId = table.Column<long>("INTEGER", nullable: false),
                Type = table.Column<int>("INTEGER", nullable: false),
                SupportingDocumentReference = table.Column<string>("TEXT", maxLength: 200, nullable: false),
                ScheduledReversalDate = table.Column<DateTime>("TEXT", nullable: true),
                ReversalJournalEntryId = table.Column<long>("INTEGER", nullable: true),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountingAdjustments", x => x.Id);
                table.ForeignKey("FK_AccountingAdjustments_JournalEntries_JournalEntryId", x => x.JournalEntryId,
                    "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AccountingAdjustments_JournalEntries_ReversalJournalEntryId", x => x.ReversalJournalEntryId,
                    "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_AccountingAdjustments_JournalEntryId", "AccountingAdjustments", "JournalEntryId", unique: true);
        migrationBuilder.CreateIndex("IX_AccountingAdjustments_ReversalJournalEntryId", "AccountingAdjustments", "ReversalJournalEntryId", unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_AccountingAdjustments_DraftOnlyInsert
            BEFORE INSERT ON AccountingAdjustments
            WHEN COALESCE((SELECT Status FROM JournalEntries WHERE Id = NEW.JournalEntryId), -1) <> 0
            BEGIN
                SELECT RAISE(ABORT, 'Adjustments must be linked while their journal is a draft');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_AccountingAdjustments_ImmutableUpdate
            BEFORE UPDATE ON AccountingAdjustments
            WHEN NEW.JournalEntryId <> OLD.JournalEntryId
                OR NEW.Type <> OLD.Type
                OR NEW.SupportingDocumentReference <> OLD.SupportingDocumentReference
                OR NEW.ScheduledReversalDate IS NOT OLD.ScheduledReversalDate
                OR NEW.CreatedAtUtc <> OLD.CreatedAtUtc
                OR NEW.CreatedBy <> OLD.CreatedBy
                OR NOT (OLD.ReversalJournalEntryId IS NULL AND NEW.ReversalJournalEntryId IS NOT NULL)
                OR NOT EXISTS (
                    SELECT 1 FROM JournalEntries
                    WHERE Id = NEW.ReversalJournalEntryId
                        AND ReversalOfJournalEntryId = OLD.JournalEntryId
                        AND Status = 2)
            BEGIN
                SELECT RAISE(ABORT, 'Accounting adjustment records are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_AccountingAdjustments_NoDeleteAfterPosting
            BEFORE DELETE ON AccountingAdjustments
            WHEN (SELECT Status FROM JournalEntries WHERE Id = OLD.JournalEntryId) IN (2, 3)
            BEGIN
                SELECT RAISE(ABORT, 'Posted accounting adjustments cannot be deleted');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("AccountingAdjustments");
}
