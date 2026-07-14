using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260715000100_AddSecurityAuditTrail")]
public sealed class AddSecurityAuditTrail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SecurityAuditEvents",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                ActorUserId = table.Column<int>(type: "INTEGER", nullable: true),
                ActorUsername = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                SubjectType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                SubjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Details = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_SecurityAuditEvents", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_SecurityAuditEvents_OccurredAtUtc",
            table: "SecurityAuditEvents",
            column: "OccurredAtUtc");
        migrationBuilder.CreateIndex(
            name: "IX_SecurityAuditEvents_Category_Action",
            table: "SecurityAuditEvents",
            columns: new[] { "Category", "Action" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("SecurityAuditEvents");
}
