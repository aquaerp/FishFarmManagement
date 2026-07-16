using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716040000_AddVatLedgerReconciliation")]
public sealed class AddVatLedgerReconciliation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE TaxInvoices ADD COLUMN OriginalTaxInvoiceId INTEGER NULL REFERENCES TaxInvoices(Id) ON DELETE RESTRICT;");
        migrationBuilder.AddColumn<string>(
            name: "AdjustmentReason",
            table: "TaxInvoices",
            type: "TEXT",
            maxLength: 500,
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_TaxInvoices_OriginalTaxInvoiceId",
            table: "TaxInvoices",
            column: "OriginalTaxInvoiceId");

        migrationBuilder.CreateTable(
            name: "VatReturnLedgerReconciliations",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                VatReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                PeriodStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                PeriodEndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                DeclaredOutputVat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                DeclaredInputVat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                LedgerOutputVat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                LedgerInputVat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                OutputDifference = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                InputDifference = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                SalesPostingCount = table.Column<int>(type: "INTEGER", nullable: false),
                PurchasePostingCount = table.Column<int>(type: "INTEGER", nullable: false),
                CreditNotePostingCount = table.Column<int>(type: "INTEGER", nullable: false),
                DebitNotePostingCount = table.Column<int>(type: "INTEGER", nullable: false),
                IsReconciled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VatReturnLedgerReconciliations", x => x.Id);
                table.ForeignKey("FK_VatReturnLedgerReconciliations_VATReturns_VatReturnId", x => x.VatReturnId,
                    "VATReturns", "Id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_VatReturnLedgerReconciliation_Period", "PeriodStartDate <= PeriodEndDate");
                table.CheckConstraint("CK_VatReturnLedgerReconciliation_Version", "Version > 0");
                table.CheckConstraint("CK_VatReturnLedgerReconciliation_Counts",
                    "SalesPostingCount >= 0 AND PurchasePostingCount >= 0 AND CreditNotePostingCount >= 0 AND DebitNotePostingCount >= 0");
            });
        migrationBuilder.CreateIndex(
            name: "IX_VatReturnLedgerReconciliations_VatReturnId_Version",
            table: "VatReturnLedgerReconciliations",
            columns: new[] { "VatReturnId", "Version" },
            unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TaxInvoices_AdjustmentShape_Insert
            BEFORE INSERT ON TaxInvoices
            WHEN NOT ((NEW.InvoiceType IN (3, 4) AND NEW.OriginalTaxInvoiceId IS NOT NULL AND length(trim(NEW.AdjustmentReason)) > 0)
                   OR (NEW.InvoiceType IN (1, 2) AND NEW.OriginalTaxInvoiceId IS NULL))
            BEGIN SELECT RAISE(ABORT, 'tax adjustment requires an original invoice and reason'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TaxInvoices_AdjustmentShape_Update
            BEFORE UPDATE ON TaxInvoices
            WHEN NOT ((NEW.InvoiceType IN (3, 4) AND NEW.OriginalTaxInvoiceId IS NOT NULL AND length(trim(NEW.AdjustmentReason)) > 0)
                   OR (NEW.InvoiceType IN (1, 2) AND NEW.OriginalTaxInvoiceId IS NULL))
            BEGIN SELECT RAISE(ABORT, 'tax adjustment requires an original invoice and reason'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TaxInvoices_PostedAdjustment_Immutable
            BEFORE UPDATE ON TaxInvoices
            WHEN EXISTS (SELECT 1 FROM OperationalPostingRecords r
                         WHERE r.SourceEntityType = 'TaxInvoice' AND r.SourceEntityId = CAST(OLD.Id AS TEXT))
            BEGIN SELECT RAISE(ABORT, 'posted tax adjustment is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_TaxInvoices_PostedAdjustment_NoDelete
            BEFORE DELETE ON TaxInvoices
            WHEN EXISTS (SELECT 1 FROM OperationalPostingRecords r
                         WHERE r.SourceEntityType = 'TaxInvoice' AND r.SourceEntityId = CAST(OLD.Id AS TEXT))
            BEGIN SELECT RAISE(ABORT, 'posted tax adjustment cannot be deleted'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_VatReturnLedgerReconciliations_Immutable
            BEFORE UPDATE ON VatReturnLedgerReconciliations
            BEGIN SELECT RAISE(ABORT, 'VAT ledger reconciliation is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_VatReturnLedgerReconciliations_NoDelete
            BEFORE DELETE ON VatReturnLedgerReconciliations
            BEGIN SELECT RAISE(ABORT, 'VAT ledger reconciliation cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_VatReturnLedgerReconciliations_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_VatReturnLedgerReconciliations_Immutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TaxInvoices_PostedAdjustment_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TaxInvoices_PostedAdjustment_Immutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TaxInvoices_AdjustmentShape_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_TaxInvoices_AdjustmentShape_Insert;");
        migrationBuilder.DropTable(name: "VatReturnLedgerReconciliations");
        migrationBuilder.DropIndex(name: "IX_TaxInvoices_OriginalTaxInvoiceId", table: "TaxInvoices");
        migrationBuilder.DropColumn(name: "AdjustmentReason", table: "TaxInvoices");
        migrationBuilder.DropColumn(name: "OriginalTaxInvoiceId", table: "TaxInvoices");
    }
}
