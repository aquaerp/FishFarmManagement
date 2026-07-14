using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715080000_AddJournalForeignCurrencyMeasurements")]
public sealed class AddJournalForeignCurrencyMeasurements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "ForeignCurrencyCode", "JournalEntryLines", "TEXT", maxLength: 3, nullable: true);
        migrationBuilder.AddColumn<decimal>(
            "ForeignAmount", "JournalEntryLines", "TEXT", precision: 18, scale: 8, nullable: true);
        migrationBuilder.AddColumn<long>(
            "ForeignExchangeRateId", "JournalEntryLines", "INTEGER", nullable: true);
        migrationBuilder.AddColumn<decimal>(
            "ExchangeRateSarPerUnit", "JournalEntryLines", "TEXT", precision: 18, scale: 8, nullable: true);
        migrationBuilder.CreateIndex(
            "IX_JournalEntryLines_ForeignExchangeRateId", "JournalEntryLines", "ForeignExchangeRateId");
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_ForeignMeasurementInsert
            BEFORE INSERT ON JournalEntryLines
            WHEN NOT (
                (NEW.ForeignCurrencyCode IS NULL AND NEW.ForeignAmount IS NULL
                    AND NEW.ForeignExchangeRateId IS NULL AND NEW.ExchangeRateSarPerUnit IS NULL)
                OR
                (NEW.ForeignCurrencyCode IS NOT NULL AND NEW.ForeignAmount IS NOT NULL
                    AND NEW.ForeignExchangeRateId IS NOT NULL AND NEW.ExchangeRateSarPerUnit IS NOT NULL
                    AND UPPER(NEW.ForeignCurrencyCode) <> 'SAR'
                    AND CAST(NEW.ForeignAmount AS NUMERIC) > 0
                    AND (EXISTS (
                        SELECT 1 FROM ForeignExchangeRates rate
                        JOIN JournalEntries entry ON entry.Id = NEW.JournalEntryId
                        WHERE rate.Id = NEW.ForeignExchangeRateId
                            AND rate.Status = 1
                            AND rate.Purpose = 0
                            AND UPPER(rate.CurrencyCode) = UPPER(NEW.ForeignCurrencyCode)
                            AND date(rate.RateDate) = date(entry.EntryDate)
                            AND CAST(rate.SarPerUnit AS NUMERIC) = CAST(NEW.ExchangeRateSarPerUnit AS NUMERIC)
                            AND ROUND(CAST(NEW.ForeignAmount AS NUMERIC) * CAST(rate.SarPerUnit AS NUMERIC), 2)
                                = CAST(NEW.Debit AS NUMERIC) + CAST(NEW.Credit AS NUMERIC))
                        OR EXISTS (
                            SELECT 1 FROM JournalEntries reversal
                            JOIN JournalEntryLines originalLine
                                ON originalLine.JournalEntryId = reversal.ReversalOfJournalEntryId
                            WHERE reversal.Id = NEW.JournalEntryId
                                AND reversal.Source = 'Reversal'
                                AND originalLine.LedgerAccountId = NEW.LedgerAccountId
                                AND originalLine.ForeignCurrencyCode = NEW.ForeignCurrencyCode
                                AND originalLine.ForeignAmount = NEW.ForeignAmount
                                AND originalLine.ForeignExchangeRateId = NEW.ForeignExchangeRateId
                                AND originalLine.ExchangeRateSarPerUnit = NEW.ExchangeRateSarPerUnit
                                AND originalLine.Debit = NEW.Credit
                                AND originalLine.Credit = NEW.Debit)))
            )
            BEGIN
                SELECT RAISE(ABORT, 'Invalid or unapproved foreign currency measurement');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_ForeignMeasurementUpdate
            BEFORE UPDATE OF ForeignCurrencyCode, ForeignAmount, ForeignExchangeRateId, ExchangeRateSarPerUnit, Debit, Credit, JournalEntryId
            ON JournalEntryLines
            WHEN NOT (
                (NEW.ForeignCurrencyCode IS NULL AND NEW.ForeignAmount IS NULL
                    AND NEW.ForeignExchangeRateId IS NULL AND NEW.ExchangeRateSarPerUnit IS NULL)
                OR
                (NEW.ForeignCurrencyCode IS NOT NULL AND NEW.ForeignAmount IS NOT NULL
                    AND NEW.ForeignExchangeRateId IS NOT NULL AND NEW.ExchangeRateSarPerUnit IS NOT NULL
                    AND UPPER(NEW.ForeignCurrencyCode) <> 'SAR'
                    AND CAST(NEW.ForeignAmount AS NUMERIC) > 0
                    AND (EXISTS (
                        SELECT 1 FROM ForeignExchangeRates rate
                        JOIN JournalEntries entry ON entry.Id = NEW.JournalEntryId
                        WHERE rate.Id = NEW.ForeignExchangeRateId
                            AND rate.Status = 1
                            AND rate.Purpose = 0
                            AND UPPER(rate.CurrencyCode) = UPPER(NEW.ForeignCurrencyCode)
                            AND date(rate.RateDate) = date(entry.EntryDate)
                            AND CAST(rate.SarPerUnit AS NUMERIC) = CAST(NEW.ExchangeRateSarPerUnit AS NUMERIC)
                            AND ROUND(CAST(NEW.ForeignAmount AS NUMERIC) * CAST(rate.SarPerUnit AS NUMERIC), 2)
                                = CAST(NEW.Debit AS NUMERIC) + CAST(NEW.Credit AS NUMERIC))
                        OR EXISTS (
                            SELECT 1 FROM JournalEntries reversal
                            JOIN JournalEntryLines originalLine
                                ON originalLine.JournalEntryId = reversal.ReversalOfJournalEntryId
                            WHERE reversal.Id = NEW.JournalEntryId
                                AND reversal.Source = 'Reversal'
                                AND originalLine.LedgerAccountId = NEW.LedgerAccountId
                                AND originalLine.ForeignCurrencyCode = NEW.ForeignCurrencyCode
                                AND originalLine.ForeignAmount = NEW.ForeignAmount
                                AND originalLine.ForeignExchangeRateId = NEW.ForeignExchangeRateId
                                AND originalLine.ExchangeRateSarPerUnit = NEW.ExchangeRateSarPerUnit
                                AND originalLine.Debit = NEW.Credit
                                AND originalLine.Credit = NEW.Debit)))
            )
            BEGIN
                SELECT RAISE(ABORT, 'Invalid or unapproved foreign currency measurement');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_JournalEntryLines_ForeignMeasurementInsert;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_JournalEntryLines_ForeignMeasurementUpdate;");
        migrationBuilder.DropIndex("IX_JournalEntryLines_ForeignExchangeRateId", "JournalEntryLines");
        migrationBuilder.DropColumn("ForeignCurrencyCode", "JournalEntryLines");
        migrationBuilder.DropColumn("ForeignAmount", "JournalEntryLines");
        migrationBuilder.DropColumn("ForeignExchangeRateId", "JournalEntryLines");
        migrationBuilder.DropColumn("ExchangeRateSarPerUnit", "JournalEntryLines");
    }
}
