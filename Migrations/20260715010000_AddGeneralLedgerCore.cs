using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715010000_AddGeneralLedgerCore")]
public sealed class AddGeneralLedgerCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "AccountingAuditEvents",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                OccurredAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                EntityType = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                EntityId = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Action = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                ActorUsername = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Reason = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                BeforeJson = table.Column<string>("TEXT", maxLength: 8000, nullable: true),
                AfterJson = table.Column<string>("TEXT", maxLength: 8000, nullable: true),
                CorrelationId = table.Column<string>("TEXT", maxLength: 64, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AccountingAuditEvents", x => x.Id));

        migrationBuilder.CreateTable(
            "AccountingSequences",
            table => new
            {
                Name = table.Column<string>("TEXT", maxLength: 50, nullable: false),
                NextValue = table.Column<long>("INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AccountingSequences", x => x.Name));

        migrationBuilder.CreateTable(
            "CostCenters",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>("TEXT", maxLength: 30, nullable: false),
                Name = table.Column<string>("TEXT", maxLength: 200, nullable: false),
                Type = table.Column<int>("INTEGER", nullable: false),
                ExternalReference = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                IsActive = table.Column<bool>("INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CostCenters", x => x.Id));

        migrationBuilder.CreateTable(
            "FiscalYears",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>("TEXT", maxLength: 50, nullable: false),
                StartDate = table.Column<DateTime>("TEXT", nullable: false),
                EndDate = table.Column<DateTime>("TEXT", nullable: false),
                IsClosed = table.Column<bool>("INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_FiscalYears", x => x.Id));

        migrationBuilder.CreateTable(
            "LedgerAccounts",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>("TEXT", maxLength: 30, nullable: false),
                NameAr = table.Column<string>("TEXT", maxLength: 200, nullable: false),
                NameEn = table.Column<string>("TEXT", maxLength: 200, nullable: true),
                Type = table.Column<int>("INTEGER", nullable: false),
                NormalBalance = table.Column<int>("INTEGER", nullable: false),
                CurrencyCode = table.Column<string>("TEXT", maxLength: 3, nullable: false),
                IsActive = table.Column<bool>("INTEGER", nullable: false),
                AllowsPosting = table.Column<bool>("INTEGER", nullable: false),
                ParentAccountId = table.Column<int>("INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerAccounts", x => x.Id);
                table.ForeignKey("FK_LedgerAccounts_LedgerAccounts_ParentAccountId", x => x.ParentAccountId,
                    "LedgerAccounts", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "FiscalPeriods",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                FiscalYearId = table.Column<int>("INTEGER", nullable: false),
                Name = table.Column<string>("TEXT", maxLength: 50, nullable: false),
                StartDate = table.Column<DateTime>("TEXT", nullable: false),
                EndDate = table.Column<DateTime>("TEXT", nullable: false),
                Status = table.Column<int>("INTEGER", nullable: false),
                ClosedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ClosedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FiscalPeriods", x => x.Id);
                table.ForeignKey("FK_FiscalPeriods_FiscalYears_FiscalYearId", x => x.FiscalYearId,
                    "FiscalYears", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "JournalEntries",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                SequenceNumber = table.Column<long>("INTEGER", nullable: false),
                EntryNumber = table.Column<string>("TEXT", maxLength: 30, nullable: false),
                EntryDate = table.Column<DateTime>("TEXT", nullable: false),
                FiscalPeriodId = table.Column<int>("INTEGER", nullable: false),
                Description = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                Source = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Reference = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                Status = table.Column<int>("INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                ApprovedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ApprovedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                PostedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                PostedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                ReversedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ReversedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true),
                ReversalOfJournalEntryId = table.Column<long>("INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JournalEntries", x => x.Id);
                table.ForeignKey("FK_JournalEntries_FiscalPeriods_FiscalPeriodId", x => x.FiscalPeriodId,
                    "FiscalPeriods", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_JournalEntries_JournalEntries_ReversalOfJournalEntryId", x => x.ReversalOfJournalEntryId,
                    "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "JournalEntryLines",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                JournalEntryId = table.Column<long>("INTEGER", nullable: false),
                LineNumber = table.Column<int>("INTEGER", nullable: false),
                LedgerAccountId = table.Column<int>("INTEGER", nullable: false),
                Debit = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                Credit = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                CostCenterId = table.Column<int>("INTEGER", nullable: true),
                Description = table.Column<string>("TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                table.CheckConstraint("CK_JournalEntryLine_NonNegative", "CAST(Debit AS NUMERIC) >= 0 AND CAST(Credit AS NUMERIC) >= 0");
                table.CheckConstraint("CK_JournalEntryLine_OneSide", "(CAST(Debit AS NUMERIC) > 0 AND CAST(Credit AS NUMERIC) = 0) OR (CAST(Credit AS NUMERIC) > 0 AND CAST(Debit AS NUMERIC) = 0)");
                table.ForeignKey("FK_JournalEntryLines_CostCenters_CostCenterId", x => x.CostCenterId,
                    "CostCenters", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_JournalEntryLines_JournalEntries_JournalEntryId", x => x.JournalEntryId,
                    "JournalEntries", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_JournalEntryLines_LedgerAccounts_LedgerAccountId", x => x.LedgerAccountId,
                    "LedgerAccounts", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_AccountingAuditEvents_EntityType_EntityId_OccurredAtUtc",
            "AccountingAuditEvents", new[] { "EntityType", "EntityId", "OccurredAtUtc" });
        migrationBuilder.CreateIndex("IX_CostCenters_Code", "CostCenters", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_FiscalPeriods_FiscalYearId_StartDate_EndDate", "FiscalPeriods",
            new[] { "FiscalYearId", "StartDate", "EndDate" }, unique: true);
        migrationBuilder.CreateIndex("IX_FiscalYears_Name", "FiscalYears", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_JournalEntries_EntryNumber", "JournalEntries", "EntryNumber", unique: true);
        migrationBuilder.CreateIndex("IX_JournalEntries_FiscalPeriodId", "JournalEntries", "FiscalPeriodId");
        migrationBuilder.CreateIndex("IX_JournalEntries_ReversalOfJournalEntryId", "JournalEntries", "ReversalOfJournalEntryId");
        migrationBuilder.CreateIndex("IX_JournalEntries_SequenceNumber", "JournalEntries", "SequenceNumber", unique: true);
        migrationBuilder.CreateIndex("IX_JournalEntryLines_CostCenterId", "JournalEntryLines", "CostCenterId");
        migrationBuilder.CreateIndex("IX_JournalEntryLines_JournalEntryId_LineNumber", "JournalEntryLines",
            new[] { "JournalEntryId", "LineNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_JournalEntryLines_LedgerAccountId", "JournalEntryLines", "LedgerAccountId");
        migrationBuilder.CreateIndex("IX_LedgerAccounts_Code", "LedgerAccounts", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_LedgerAccounts_ParentAccountId", "LedgerAccounts", "ParentAccountId");

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntries_Immutable
            BEFORE UPDATE ON JournalEntries
            WHEN OLD.Status IN (2, 3) AND (
                OLD.Status = 3 OR NEW.Status <> 3 OR
                NEW.SequenceNumber <> OLD.SequenceNumber OR
                NEW.EntryNumber <> OLD.EntryNumber OR
                NEW.EntryDate <> OLD.EntryDate OR
                NEW.FiscalPeriodId <> OLD.FiscalPeriodId OR
                NEW.Description <> OLD.Description OR
                NEW.Source <> OLD.Source OR
                NEW.Reference IS NOT OLD.Reference OR
                NEW.CreatedAtUtc <> OLD.CreatedAtUtc OR
                NEW.CreatedBy <> OLD.CreatedBy OR
                NEW.ApprovedAtUtc IS NOT OLD.ApprovedAtUtc OR
                NEW.ApprovedBy IS NOT OLD.ApprovedBy OR
                NEW.PostedAtUtc IS NOT OLD.PostedAtUtc OR
                NEW.PostedBy IS NOT OLD.PostedBy OR
                NEW.ReversalOfJournalEntryId IS NOT OLD.ReversalOfJournalEntryId
            )
            BEGIN
                SELECT RAISE(ABORT, 'Posted journal entries are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntries_NoDelete
            BEFORE DELETE ON JournalEntries
            WHEN OLD.Status IN (2, 3)
            BEGIN
                SELECT RAISE(ABORT, 'Posted journal entries cannot be deleted');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_NoInsertAfterPosting
            BEFORE INSERT ON JournalEntryLines
            WHEN (SELECT Status FROM JournalEntries WHERE Id = NEW.JournalEntryId) IN (2, 3)
            BEGIN
                SELECT RAISE(ABORT, 'Posted journal entry lines are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_NoUpdateAfterPosting
            BEFORE UPDATE ON JournalEntryLines
            WHEN (SELECT Status FROM JournalEntries WHERE Id = OLD.JournalEntryId) IN (2, 3)
            BEGIN
                SELECT RAISE(ABORT, 'Posted journal entry lines are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_NoDeleteAfterPosting
            BEFORE DELETE ON JournalEntryLines
            WHEN (SELECT Status FROM JournalEntries WHERE Id = OLD.JournalEntryId) IN (2, 3)
            BEGIN
                SELECT RAISE(ABORT, 'Posted journal entry lines are immutable');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AccountingAuditEvents");
        migrationBuilder.DropTable("AccountingSequences");
        migrationBuilder.DropTable("JournalEntryLines");
        migrationBuilder.DropTable("CostCenters");
        migrationBuilder.DropTable("JournalEntries");
        migrationBuilder.DropTable("LedgerAccounts");
        migrationBuilder.DropTable("FiscalPeriods");
        migrationBuilder.DropTable("FiscalYears");
    }
}
