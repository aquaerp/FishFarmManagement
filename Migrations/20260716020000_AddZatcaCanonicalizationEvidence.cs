using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716020000_AddZatcaCanonicalizationEvidence")]
public sealed class AddZatcaCanonicalizationEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ZatcaCanonicalizationEvidence", table => new
        {
            Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            ZatcaDocumentEnvelopeId = table.Column<long>("INTEGER", nullable: false),
            CanonicalizationAlgorithm = table.Column<string>("TEXT", maxLength: 200, nullable: false),
            DigestAlgorithm = table.Column<string>("TEXT", maxLength: 200, nullable: false),
            InvoiceHashHex = table.Column<string>("TEXT", maxLength: 64, nullable: false),
            InvoiceHashBase64 = table.Column<string>("TEXT", maxLength: 44, nullable: false),
            CanonicalXmlSha256Base64 = table.Column<string>("TEXT", maxLength: 44, nullable: false),
            CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            Reason = table.Column<string>("TEXT", maxLength: 500, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ZatcaCanonicalizationEvidence", value => value.Id);
            table.ForeignKey("FK_ZatcaCanonicalizationEvidence_ZatcaDocumentEnvelopes_ZatcaDocumentEnvelopeId",
                value => value.ZatcaDocumentEnvelopeId, "ZatcaDocumentEnvelopes", "Id",
                onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_ZatcaCanonicalizationEvidence_Envelope",
            "ZatcaCanonicalizationEvidence", "ZatcaDocumentEnvelopeId", unique: true);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaCanonicalizationEvidence_Immutable
            BEFORE UPDATE ON ZatcaCanonicalizationEvidence BEGIN SELECT RAISE(ABORT, 'ZATCA canonicalization evidence is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaCanonicalizationEvidence_NoDelete
            BEFORE DELETE ON ZatcaCanonicalizationEvidence BEGIN SELECT RAISE(ABORT, 'ZATCA canonicalization evidence cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaCanonicalizationEvidence_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaCanonicalizationEvidence_Immutable;");
        migrationBuilder.DropTable("ZatcaCanonicalizationEvidence");
    }
}
