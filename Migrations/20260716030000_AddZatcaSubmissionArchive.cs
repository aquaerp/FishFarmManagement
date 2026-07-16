using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716030000_AddZatcaSubmissionArchive")]
public sealed class AddZatcaSubmissionArchive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ZatcaSubmissionArchives", table => new
        {
            Id = table.Column<long>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            ZatcaDocumentEnvelopeId = table.Column<long>("INTEGER", nullable: false),
            AttemptNumber = table.Column<int>("INTEGER", nullable: false),
            Route = table.Column<int>("INTEGER", nullable: false),
            EndpointPath = table.Column<string>("TEXT", maxLength: 200, nullable: false),
            IdempotencyKey = table.Column<string>("TEXT", maxLength: 64, nullable: false),
            RequestPayloadJson = table.Column<string>("TEXT", nullable: false),
            RequestSha256Base64 = table.Column<string>("TEXT", maxLength: 44, nullable: false),
            SubmittedXml = table.Column<string>("TEXT", nullable: false),
            ResponseBody = table.Column<string>("TEXT", nullable: false),
            ResponseSha256Base64 = table.Column<string>("TEXT", maxLength: 44, nullable: false),
            HttpStatusCode = table.Column<int>("INTEGER", nullable: true),
            Disposition = table.Column<int>("INTEGER", nullable: false),
            AuthorityStatus = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            IsAccepted = table.Column<bool>("INTEGER", nullable: false),
            IsRetryable = table.Column<bool>("INTEGER", nullable: false),
            StartedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            CompletedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            DurationMilliseconds = table.Column<long>("INTEGER", nullable: false),
            CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false),
            CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            Reason = table.Column<string>("TEXT", maxLength: 500, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ZatcaSubmissionArchives", value => value.Id);
            table.ForeignKey("FK_ZatcaSubmissionArchives_ZatcaDocumentEnvelopes_ZatcaDocumentEnvelopeId",
                value => value.ZatcaDocumentEnvelopeId, "ZatcaDocumentEnvelopes", "Id", onDelete: ReferentialAction.Restrict);
            table.CheckConstraint("CK_ZatcaSubmissionArchive_Attempt", "AttemptNumber > 0");
            table.CheckConstraint("CK_ZatcaSubmissionArchive_Duration", "DurationMilliseconds >= 0");
        });
        migrationBuilder.CreateIndex("IX_ZatcaSubmissionArchives_EnvelopeAttempt", "ZatcaSubmissionArchives",
            new[] { "ZatcaDocumentEnvelopeId", "AttemptNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_ZatcaSubmissionArchives_IdempotencyKey", "ZatcaSubmissionArchives", "IdempotencyKey");
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaSubmissionArchives_Immutable
            BEFORE UPDATE ON ZatcaSubmissionArchives BEGIN SELECT RAISE(ABORT, 'ZATCA submission archive is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_ZatcaSubmissionArchives_NoDelete
            BEFORE DELETE ON ZatcaSubmissionArchives BEGIN SELECT RAISE(ABORT, 'ZATCA submission archive cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaSubmissionArchives_NoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_ZatcaSubmissionArchives_Immutable;");
        migrationBuilder.DropTable("ZatcaSubmissionArchives");
    }
}
