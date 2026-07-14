using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715100000_AddForeignCurrencyRevaluations")]
public sealed class AddForeignCurrencyRevaluations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ForeignCurrencyRevaluations",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ForeignMonetaryItemId = table.Column<long>("INTEGER", nullable: false),
                RevaluationDate = table.Column<DateTime>("TEXT", nullable: false),
                PreviousCarryingAmountSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                RevaluedCarryingAmountSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                UnrealizedGainLossSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                ForeignExchangeRateId = table.Column<long>("INTEGER", nullable: false),
                JournalEntryId = table.Column<long>("INTEGER", nullable: true),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ForeignCurrencyRevaluations", x => x.Id);
                table.ForeignKey("FK_ForeignCurrencyRevaluations_ForeignMonetaryItems_ForeignMonetaryItemId",
                    x => x.ForeignMonetaryItemId, "ForeignMonetaryItems", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ForeignCurrencyRevaluations_ForeignExchangeRates_ForeignExchangeRateId",
                    x => x.ForeignExchangeRateId, "ForeignExchangeRates", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ForeignCurrencyRevaluations_JournalEntries_JournalEntryId",
                    x => x.JournalEntryId, "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_ForeignCurrencyRevaluations_ForeignExchangeRateId",
            "ForeignCurrencyRevaluations", "ForeignExchangeRateId");
        migrationBuilder.CreateIndex("IX_ForeignCurrencyRevaluations_ForeignMonetaryItemId_RevaluationDate",
            "ForeignCurrencyRevaluations", new[] { "ForeignMonetaryItemId", "RevaluationDate" }, unique: true);
        migrationBuilder.CreateIndex("IX_ForeignCurrencyRevaluations_JournalEntryId",
            "ForeignCurrencyRevaluations", "JournalEntryId", unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignCurrencyRevaluations_ValidInsert
            BEFORE INSERT ON ForeignCurrencyRevaluations
            WHEN CAST(NEW.PreviousCarryingAmountSar AS NUMERIC) < 0
                OR CAST(NEW.RevaluedCarryingAmountSar AS NUMERIC) < 0
                OR NOT EXISTS (
                    SELECT 1 FROM ForeignMonetaryItems item
                    JOIN ForeignExchangeRates rate ON rate.Id = NEW.ForeignExchangeRateId
                    WHERE item.Id = NEW.ForeignMonetaryItemId
                        AND rate.Purpose = 1
                        AND rate.CurrencyCode = item.CurrencyCode
                        AND date(rate.RateDate) = date(NEW.RevaluationDate)
                        AND CAST(NEW.RevaluedCarryingAmountSar AS NUMERIC)
                            = ROUND(CAST(item.OutstandingForeignAmount AS NUMERIC) * CAST(rate.SarPerUnit AS NUMERIC), 2)
                        AND CAST(NEW.UnrealizedGainLossSar AS NUMERIC) = CASE
                            WHEN item.Kind = 0 THEN CAST(NEW.RevaluedCarryingAmountSar AS NUMERIC)
                                - CAST(NEW.PreviousCarryingAmountSar AS NUMERIC)
                            ELSE CAST(NEW.PreviousCarryingAmountSar AS NUMERIC)
                                - CAST(NEW.RevaluedCarryingAmountSar AS NUMERIC) END)
                OR NOT (
                    (CAST(NEW.UnrealizedGainLossSar AS NUMERIC) = 0 AND NEW.JournalEntryId IS NULL)
                    OR (CAST(NEW.UnrealizedGainLossSar AS NUMERIC) <> 0 AND EXISTS (
                        SELECT 1 FROM JournalEntries WHERE Id = NEW.JournalEntryId
                            AND Status = 2 AND Source = 'ForeignCurrencyRevaluation')))
            BEGIN
                SELECT RAISE(ABORT, 'Invalid foreign currency closing revaluation');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignCurrencyRevaluations_ImmutableUpdate
            BEFORE UPDATE ON ForeignCurrencyRevaluations
            BEGIN
                SELECT RAISE(ABORT, 'Foreign currency revaluations are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignCurrencyRevaluations_NoDelete
            BEFORE DELETE ON ForeignCurrencyRevaluations
            BEGIN
                SELECT RAISE(ABORT, 'Foreign currency revaluations cannot be deleted');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("ForeignCurrencyRevaluations");
}
