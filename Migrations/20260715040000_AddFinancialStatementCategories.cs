using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715040000_AddFinancialStatementCategories")]
public sealed class AddFinancialStatementCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            "FinancialStatementCategory",
            "LedgerAccounts",
            "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE LedgerAccounts SET FinancialStatementCategory = CASE Code
                WHEN '1110' THEN 1
                WHEN '1120' THEN 2
                WHEN '1130' THEN 3
                WHEN '1140' THEN 4
                WHEN '1510' THEN 5
                WHEN '1520' THEN 6
                WHEN '2100' THEN 8
                WHEN '2110' THEN 9
                WHEN '2200' THEN 10
                WHEN '2120' THEN 11
                WHEN '3100' THEN 14
                WHEN '3200' THEN 15
                WHEN '4100' THEN 17
                WHEN '5100' THEN 19
                WHEN '5200' THEN 20
                WHEN '5300' THEN 21
                ELSE FinancialStatementCategory
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("FinancialStatementCategory", "LedgerAccounts");
}
