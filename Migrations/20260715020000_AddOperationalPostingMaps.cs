using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715020000_AddOperationalPostingMaps")]
public sealed class AddOperationalPostingMaps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "AccountingConfigurations",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>("TEXT", maxLength: 200, nullable: false),
                Version = table.Column<int>("INTEGER", nullable: false),
                Status = table.Column<int>("INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                ApprovedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ApprovedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AccountingConfigurations", x => x.Id));

        migrationBuilder.CreateTable(
            "OperationalPostingRecords",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                EventType = table.Column<int>("INTEGER", nullable: false),
                SourceEntityType = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                SourceEntityId = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                JournalEntryId = table.Column<long>("INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OperationalPostingRecords", x => x.Id);
                table.ForeignKey("FK_OperationalPostingRecords_JournalEntries_JournalEntryId", x => x.JournalEntryId,
                    "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PostingMappings",
            table => new
            {
                Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                AccountingConfigurationId = table.Column<int>("INTEGER", nullable: false),
                EventType = table.Column<int>("INTEGER", nullable: false),
                Component = table.Column<int>("INTEGER", nullable: false),
                LedgerAccountId = table.Column<int>("INTEGER", nullable: false),
                IsActive = table.Column<bool>("INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PostingMappings", x => x.Id);
                table.ForeignKey("FK_PostingMappings_AccountingConfigurations_AccountingConfigurationId",
                    x => x.AccountingConfigurationId, "AccountingConfigurations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_PostingMappings_LedgerAccounts_LedgerAccountId",
                    x => x.LedgerAccountId, "LedgerAccounts", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_AccountingConfigurations_Name_Version", "AccountingConfigurations",
            new[] { "Name", "Version" }, unique: true);
        migrationBuilder.CreateIndex("IX_OperationalPostingRecords_EventType_SourceEntityType_SourceEntityId",
            "OperationalPostingRecords", new[] { "EventType", "SourceEntityType", "SourceEntityId" }, unique: true);
        migrationBuilder.CreateIndex("IX_OperationalPostingRecords_JournalEntryId", "OperationalPostingRecords",
            "JournalEntryId", unique: true);
        migrationBuilder.CreateIndex("IX_PostingMappings_AccountingConfigurationId_EventType_Component",
            "PostingMappings", new[] { "AccountingConfigurationId", "EventType", "Component" }, unique: true);
        migrationBuilder.CreateIndex("IX_PostingMappings_LedgerAccountId", "PostingMappings", "LedgerAccountId");

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_AccountingConfigurations_Immutable
            BEFORE UPDATE ON AccountingConfigurations
            WHEN OLD.Status IN (1, 2) AND NOT (
                OLD.Status = 1 AND NEW.Status = 2 AND
                NEW.Name = OLD.Name AND NEW.Version = OLD.Version AND
                NEW.CreatedAtUtc = OLD.CreatedAtUtc AND NEW.CreatedBy = OLD.CreatedBy AND
                NEW.ApprovedAtUtc IS OLD.ApprovedAtUtc AND NEW.ApprovedBy IS OLD.ApprovedBy
            )
            BEGIN
                SELECT RAISE(ABORT, 'Approved accounting configurations are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_PostingMappings_NoInsertAfterApproval
            BEFORE INSERT ON PostingMappings
            WHEN (SELECT Status FROM AccountingConfigurations WHERE Id = NEW.AccountingConfigurationId) IN (1, 2)
            BEGIN
                SELECT RAISE(ABORT, 'Approved posting mappings are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_PostingMappings_NoUpdateAfterApproval
            BEFORE UPDATE ON PostingMappings
            WHEN (SELECT Status FROM AccountingConfigurations WHERE Id = OLD.AccountingConfigurationId) IN (1, 2)
            BEGIN
                SELECT RAISE(ABORT, 'Approved posting mappings are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_PostingMappings_NoDeleteAfterApproval
            BEFORE DELETE ON PostingMappings
            WHEN (SELECT Status FROM AccountingConfigurations WHERE Id = OLD.AccountingConfigurationId) IN (1, 2)
            BEGIN
                SELECT RAISE(ABORT, 'Approved posting mappings are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_OperationalPostingRecords_ImmutableUpdate
            BEFORE UPDATE ON OperationalPostingRecords
            BEGIN
                SELECT RAISE(ABORT, 'Operational posting records are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_OperationalPostingRecords_ImmutableDelete
            BEFORE DELETE ON OperationalPostingRecords
            BEGIN
                SELECT RAISE(ABORT, 'Operational posting records cannot be deleted');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OperationalPostingRecords");
        migrationBuilder.DropTable("PostingMappings");
        migrationBuilder.DropTable("AccountingConfigurations");
    }
}
