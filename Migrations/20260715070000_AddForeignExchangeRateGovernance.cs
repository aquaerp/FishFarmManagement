using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715070000_AddForeignExchangeRateGovernance")]
public sealed class AddForeignExchangeRateGovernance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ForeignExchangeRates",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                CurrencyCode = table.Column<string>("TEXT", maxLength: 3, nullable: false),
                RateDate = table.Column<DateTime>("TEXT", nullable: false),
                Purpose = table.Column<int>("INTEGER", nullable: false),
                Version = table.Column<int>("INTEGER", nullable: false),
                SarPerUnit = table.Column<decimal>("TEXT", precision: 18, scale: 8, nullable: false),
                SourceReference = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                EvidenceReference = table.Column<string>("TEXT", maxLength: 500, nullable: false),
                Status = table.Column<int>("INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                ApprovedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
                ApprovedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ForeignExchangeRates", x => x.Id);
                table.CheckConstraint("CK_ForeignExchangeRate_Positive", "CAST(SarPerUnit AS NUMERIC) > 0");
            });

        migrationBuilder.CreateIndex(
            "IX_ForeignExchangeRates_CurrencyCode_RateDate_Purpose_Version",
            "ForeignExchangeRates",
            new[] { "CurrencyCode", "RateDate", "Purpose", "Version" },
            unique: true);
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IX_ForeignExchangeRates_OneApproved
            ON ForeignExchangeRates (CurrencyCode, RateDate, Purpose)
            WHERE Status = 1;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignExchangeRates_DraftOnlyInsert
            BEFORE INSERT ON ForeignExchangeRates
            WHEN NEW.Status <> 0
                OR NEW.ApprovedAtUtc IS NOT NULL
                OR NEW.ApprovedBy IS NOT NULL
            BEGIN
                SELECT RAISE(ABORT, 'Foreign exchange rates must be inserted as drafts');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignExchangeRates_GovernedUpdate
            BEFORE UPDATE ON ForeignExchangeRates
            WHEN OLD.Status = 2
                OR NEW.CurrencyCode <> OLD.CurrencyCode
                OR NEW.RateDate <> OLD.RateDate
                OR NEW.Purpose <> OLD.Purpose
                OR NEW.Version <> OLD.Version
                OR NEW.SarPerUnit <> OLD.SarPerUnit
                OR NEW.SourceReference <> OLD.SourceReference
                OR NEW.EvidenceReference <> OLD.EvidenceReference
                OR NEW.CreatedAtUtc <> OLD.CreatedAtUtc
                OR NEW.CreatedBy <> OLD.CreatedBy
                OR (OLD.Status = 0 AND NOT (
                    NEW.Status = 1
                    AND NEW.ApprovedAtUtc IS NOT NULL
                    AND NEW.ApprovedBy IS NOT NULL
                    AND lower(NEW.ApprovedBy) <> lower(OLD.CreatedBy)))
                OR (OLD.Status = 1 AND NOT (
                    NEW.Status = 2
                    AND NEW.ApprovedAtUtc IS OLD.ApprovedAtUtc
                    AND NEW.ApprovedBy IS OLD.ApprovedBy))
            BEGIN
                SELECT RAISE(ABORT, 'Foreign exchange rate changes require the governed lifecycle');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignExchangeRates_ApprovedNoDelete
            BEFORE DELETE ON ForeignExchangeRates
            WHEN OLD.Status <> 0
            BEGIN
                SELECT RAISE(ABORT, 'Approved or retired foreign exchange rates cannot be deleted');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("ForeignExchangeRates");
}
