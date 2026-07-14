using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715090000_AddForeignMonetaryItems")]
public sealed class AddForeignMonetaryItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ForeignMonetaryItems",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Reference = table.Column<string>("TEXT", maxLength: 100, nullable: false),
                Kind = table.Column<int>("INTEGER", nullable: false),
                RecognitionJournalEntryLineId = table.Column<long>("INTEGER", nullable: false),
                LedgerAccountId = table.Column<int>("INTEGER", nullable: false),
                CurrencyCode = table.Column<string>("TEXT", maxLength: 3, nullable: false),
                OriginalForeignAmount = table.Column<decimal>("TEXT", precision: 18, scale: 8, nullable: false),
                OutstandingForeignAmount = table.Column<decimal>("TEXT", precision: 18, scale: 8, nullable: false),
                CarryingAmountSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                LastMeasurementDate = table.Column<DateTime>("TEXT", nullable: false),
                Status = table.Column<int>("INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ForeignMonetaryItems", x => x.Id);
                table.ForeignKey("FK_ForeignMonetaryItems_JournalEntryLines_RecognitionJournalEntryLineId",
                    x => x.RecognitionJournalEntryLineId, "JournalEntryLines", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ForeignMonetaryItems_LedgerAccounts_LedgerAccountId",
                    x => x.LedgerAccountId, "LedgerAccounts", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_ForeignMonetaryItems_Reference", "ForeignMonetaryItems", "Reference", unique: true);
        migrationBuilder.CreateIndex("IX_ForeignMonetaryItems_RecognitionJournalEntryLineId", "ForeignMonetaryItems",
            "RecognitionJournalEntryLineId", unique: true);
        migrationBuilder.CreateIndex("IX_ForeignMonetaryItems_LedgerAccountId", "ForeignMonetaryItems", "LedgerAccountId");

        migrationBuilder.CreateTable(
            "ForeignCurrencySettlements",
            table => new
            {
                Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ForeignMonetaryItemId = table.Column<long>("INTEGER", nullable: false),
                SettlementDate = table.Column<DateTime>("TEXT", nullable: false),
                ForeignAmount = table.Column<decimal>("TEXT", precision: 18, scale: 8, nullable: false),
                CarryingAmountReleasedSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                SettlementAmountSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                RealizedGainLossSar = table.Column<decimal>("TEXT", precision: 18, scale: 2, nullable: false),
                ForeignExchangeRateId = table.Column<long>("INTEGER", nullable: false),
                JournalEntryId = table.Column<long>("INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
                CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ForeignCurrencySettlements", x => x.Id);
                table.ForeignKey("FK_ForeignCurrencySettlements_ForeignMonetaryItems_ForeignMonetaryItemId",
                    x => x.ForeignMonetaryItemId, "ForeignMonetaryItems", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ForeignCurrencySettlements_ForeignExchangeRates_ForeignExchangeRateId",
                    x => x.ForeignExchangeRateId, "ForeignExchangeRates", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ForeignCurrencySettlements_JournalEntries_JournalEntryId",
                    x => x.JournalEntryId, "JournalEntries", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_ForeignCurrencySettlements_ForeignMonetaryItemId",
            "ForeignCurrencySettlements", "ForeignMonetaryItemId");
        migrationBuilder.CreateIndex("IX_ForeignCurrencySettlements_ForeignExchangeRateId",
            "ForeignCurrencySettlements", "ForeignExchangeRateId");
        migrationBuilder.CreateIndex("IX_ForeignCurrencySettlements_JournalEntryId",
            "ForeignCurrencySettlements", "JournalEntryId", unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignMonetaryItems_ValidInsert
            BEFORE INSERT ON ForeignMonetaryItems
            WHEN NEW.Status <> 0
                OR CAST(NEW.OriginalForeignAmount AS NUMERIC) <= 0
                OR CAST(NEW.OutstandingForeignAmount AS NUMERIC) <> CAST(NEW.OriginalForeignAmount AS NUMERIC)
                OR CAST(NEW.CarryingAmountSar AS NUMERIC) <= 0
                OR NOT EXISTS (
                    SELECT 1 FROM JournalEntryLines line
                    JOIN JournalEntries entry ON entry.Id = line.JournalEntryId
                    JOIN LedgerAccounts account ON account.Id = line.LedgerAccountId
                    WHERE line.Id = NEW.RecognitionJournalEntryLineId
                        AND entry.Status = 2
                        AND line.LedgerAccountId = NEW.LedgerAccountId
                        AND UPPER(line.ForeignCurrencyCode) = UPPER(NEW.CurrencyCode)
                        AND CAST(line.ForeignAmount AS NUMERIC) = CAST(NEW.OriginalForeignAmount AS NUMERIC)
                        AND CAST(line.Debit AS NUMERIC) + CAST(line.Credit AS NUMERIC)
                            = CAST(NEW.CarryingAmountSar AS NUMERIC)
                        AND date(entry.EntryDate) = date(NEW.LastMeasurementDate)
                        AND ((NEW.Kind = 0 AND account.Type = 0 AND CAST(line.Debit AS NUMERIC) > 0)
                            OR (NEW.Kind = 1 AND account.Type = 1 AND CAST(line.Credit AS NUMERIC) > 0)))
            BEGIN
                SELECT RAISE(ABORT, 'Invalid foreign monetary item registration');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignMonetaryItems_GovernedUpdate
            BEFORE UPDATE ON ForeignMonetaryItems
            WHEN NEW.Reference <> OLD.Reference
                OR NEW.Kind <> OLD.Kind
                OR NEW.RecognitionJournalEntryLineId <> OLD.RecognitionJournalEntryLineId
                OR NEW.LedgerAccountId <> OLD.LedgerAccountId
                OR NEW.CurrencyCode <> OLD.CurrencyCode
                OR NEW.OriginalForeignAmount <> OLD.OriginalForeignAmount
                OR NEW.CreatedAtUtc <> OLD.CreatedAtUtc
                OR NEW.CreatedBy <> OLD.CreatedBy
                OR CAST(NEW.OutstandingForeignAmount AS NUMERIC) < 0
                OR CAST(NEW.OutstandingForeignAmount AS NUMERIC) > CAST(OLD.OutstandingForeignAmount AS NUMERIC)
                OR CAST(NEW.CarryingAmountSar AS NUMERIC) < 0
                OR date(NEW.LastMeasurementDate) < date(OLD.LastMeasurementDate)
                OR NOT ((NEW.Status = 0 AND CAST(NEW.OutstandingForeignAmount AS NUMERIC) > 0)
                    OR (NEW.Status = 1 AND CAST(NEW.OutstandingForeignAmount AS NUMERIC) = 0
                        AND CAST(NEW.CarryingAmountSar AS NUMERIC) = 0))
            BEGIN
                SELECT RAISE(ABORT, 'Foreign monetary item changes violate the governed lifecycle');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignMonetaryItems_NoDelete
            BEFORE DELETE ON ForeignMonetaryItems
            BEGIN
                SELECT RAISE(ABORT, 'Foreign monetary items cannot be deleted');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignCurrencySettlements_ValidInsert
            BEFORE INSERT ON ForeignCurrencySettlements
            WHEN CAST(NEW.ForeignAmount AS NUMERIC) <= 0
                OR CAST(NEW.CarryingAmountReleasedSar AS NUMERIC) <= 0
                OR CAST(NEW.SettlementAmountSar AS NUMERIC) <= 0
                OR NOT EXISTS (SELECT 1 FROM JournalEntries WHERE Id = NEW.JournalEntryId
                    AND Status = 2 AND Source = 'ForeignCurrencySettlement')
                OR NOT EXISTS (SELECT 1 FROM ForeignExchangeRates WHERE Id = NEW.ForeignExchangeRateId)
            BEGIN
                SELECT RAISE(ABORT, 'Invalid realized foreign currency settlement');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignCurrencySettlements_ImmutableUpdate
            BEFORE UPDATE ON ForeignCurrencySettlements
            BEGIN
                SELECT RAISE(ABORT, 'Foreign currency settlements are immutable');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ForeignCurrencySettlements_NoDelete
            BEFORE DELETE ON ForeignCurrencySettlements
            BEGIN
                SELECT RAISE(ABORT, 'Foreign currency settlements cannot be deleted');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ForeignCurrencySettlements");
        migrationBuilder.DropTable("ForeignMonetaryItems");
    }
}
