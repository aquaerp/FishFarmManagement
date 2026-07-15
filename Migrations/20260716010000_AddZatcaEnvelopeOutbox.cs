using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716010000_AddZatcaEnvelopeOutbox")]
public sealed class AddZatcaEnvelopeOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ZatcaEgsUnits", table => new
        {
            Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            DeviceId = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            LastReservedInvoiceCounterValue = table.Column<long>("INTEGER", nullable: false),
            PreviousInvoiceHashBase64 = table.Column<string>("TEXT", maxLength: 500, nullable: false),
            HasOpenEnvelope = table.Column<bool>("INTEGER", nullable: false),
            CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ZatcaEgsUnits", value => value.Id);
            table.CheckConstraint("CK_ZatcaEgsUnit_Counter", "LastReservedInvoiceCounterValue >= 0");
        });
        migrationBuilder.CreateIndex("IX_ZatcaEgsUnits_DeviceId", "ZatcaEgsUnits", "DeviceId", unique: true);

        migrationBuilder.CreateTable("ZatcaDocumentEnvelopes", table => new
        {
            Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            ZatcaEgsUnitId = table.Column<int>("INTEGER", nullable: false),
            SourceEntityType = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            SourceEntityId = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            DocumentNumber = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            Uuid = table.Column<string>("TEXT", maxLength: 36, nullable: false),
            InvoiceCounterValue = table.Column<long>("INTEGER", nullable: false),
            PreviousInvoiceHashBase64 = table.Column<string>("TEXT", maxLength: 500, nullable: false),
            UnsignedXml = table.Column<string>("TEXT", nullable: false),
            LocalPayloadSha256Base64 = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            Profile = table.Column<int>("INTEGER", nullable: false),
            Kind = table.Column<int>("INTEGER", nullable: false),
            SubmissionRoute = table.Column<int>("INTEGER", nullable: false),
            State = table.Column<int>("INTEGER", nullable: false),
            CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            Reason = table.Column<string>("TEXT", maxLength: 500, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ZatcaDocumentEnvelopes", value => value.Id);
            table.ForeignKey("FK_ZatcaDocumentEnvelopes_ZatcaEgsUnits_ZatcaEgsUnitId",
                value => value.ZatcaEgsUnitId, "ZatcaEgsUnits", "Id", onDelete: ReferentialAction.Restrict);
            table.CheckConstraint("CK_ZatcaDocumentEnvelope_ICV", "InvoiceCounterValue > 0");
        });
        migrationBuilder.CreateIndex("IX_ZatcaDocumentEnvelopes_Uuid", "ZatcaDocumentEnvelopes", "Uuid", unique: true);
        migrationBuilder.CreateIndex("IX_ZatcaDocumentEnvelopes_Source", "ZatcaDocumentEnvelopes",
            new[] { "SourceEntityType", "SourceEntityId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ZatcaDocumentEnvelopes_EgsIcv", "ZatcaDocumentEnvelopes",
            new[] { "ZatcaEgsUnitId", "InvoiceCounterValue" }, unique: true);

        migrationBuilder.CreateTable("ZatcaOutboxMessages", table => new
        {
            Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            ZatcaDocumentEnvelopeId = table.Column<long>("INTEGER", nullable: false),
            Status = table.Column<int>("INTEGER", nullable: false),
            AttemptCount = table.Column<int>("INTEGER", nullable: false),
            NextAttemptAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            UpdatedAtUtc = table.Column<DateTime>("TEXT", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ZatcaOutboxMessages", value => value.Id);
            table.ForeignKey("FK_ZatcaOutboxMessages_ZatcaDocumentEnvelopes_ZatcaDocumentEnvelopeId",
                value => value.ZatcaDocumentEnvelopeId, "ZatcaDocumentEnvelopes", "Id",
                onDelete: ReferentialAction.Restrict);
            table.CheckConstraint("CK_ZatcaOutbox_Attempts", "AttemptCount >= 0");
        });
        migrationBuilder.CreateIndex("IX_ZatcaOutboxMessages_Envelope", "ZatcaOutboxMessages",
            "ZatcaDocumentEnvelopeId", unique: true);

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaDocumentEnvelopes_Immutable
            BEFORE UPDATE ON ZatcaDocumentEnvelopes BEGIN SELECT RAISE(ABORT, 'ZATCA envelope evidence is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaDocumentEnvelopes_NoDelete
            BEFORE DELETE ON ZatcaDocumentEnvelopes BEGIN SELECT RAISE(ABORT, 'ZATCA envelope evidence cannot be deleted'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaOutboxMessages_NoDelete
            BEFORE DELETE ON ZatcaOutboxMessages BEGIN SELECT RAISE(ABORT, 'ZATCA outbox history cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaOutboxMessages_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaDocumentEnvelopes_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaDocumentEnvelopes_Immutable;");
        migrationBuilder.DropTable("ZatcaOutboxMessages");
        migrationBuilder.DropTable("ZatcaDocumentEnvelopes");
        migrationBuilder.DropTable("ZatcaEgsUnits");
    }
}
