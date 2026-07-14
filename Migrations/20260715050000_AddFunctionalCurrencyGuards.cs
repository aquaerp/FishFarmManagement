using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715050000_AddFunctionalCurrencyGuards")]
public sealed class AddFunctionalCurrencyGuards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_SarOnlyInsert
            BEFORE INSERT ON JournalEntryLines
            WHEN UPPER(COALESCE((SELECT CurrencyCode FROM LedgerAccounts WHERE Id = NEW.LedgerAccountId), '')) <> 'SAR'
            BEGIN
                SELECT RAISE(ABORT, 'Only SAR accounts can be posted');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_JournalEntryLines_SarOnlyUpdate
            BEFORE UPDATE OF LedgerAccountId ON JournalEntryLines
            WHEN UPPER(COALESCE((SELECT CurrencyCode FROM LedgerAccounts WHERE Id = NEW.LedgerAccountId), '')) <> 'SAR'
            BEGIN
                SELECT RAISE(ABORT, 'Only SAR accounts can be posted');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_LedgerAccounts_CurrencyImmutableAfterUse
            BEFORE UPDATE OF CurrencyCode ON LedgerAccounts
            WHEN UPPER(NEW.CurrencyCode) <> UPPER(OLD.CurrencyCode)
                AND EXISTS (SELECT 1 FROM JournalEntryLines WHERE LedgerAccountId = OLD.Id)
            BEGIN
                SELECT RAISE(ABORT, 'Account currency is immutable after journal use');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_JournalEntryLines_SarOnlyInsert;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_JournalEntryLines_SarOnlyUpdate;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_LedgerAccounts_CurrencyImmutableAfterUse;");
    }
}
