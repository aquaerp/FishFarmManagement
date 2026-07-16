using System;
using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716070000_AddControlledDocuments")]
public sealed class AddControlledDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ControlledDocuments", table => new
        {
            Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            DocumentCode = table.Column<string>("TEXT", maxLength: 50, nullable: false), Version = table.Column<string>("TEXT", maxLength: 30, nullable: false),
            Title = table.Column<string>("TEXT", maxLength: 200, nullable: false), Category = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            Owner = table.Column<string>("TEXT", maxLength: 100, nullable: false), StorageReference = table.Column<string>("TEXT", maxLength: 500, nullable: false),
            ContentSha256 = table.Column<string>("TEXT", maxLength: 64, nullable: false), ChangeSummary = table.Column<string>("TEXT", maxLength: 1000, nullable: true),
            Status = table.Column<int>("INTEGER", nullable: false), EffectiveDate = table.Column<DateTime>("TEXT", nullable: true),
            ApprovedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true), ApprovedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
            ApprovalReason = table.Column<string>("TEXT", maxLength: 500, nullable: true), ObsoletedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
            ObsoletedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true), CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false), CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ControlledDocuments", x => x.Id);
            table.CheckConstraint("CK_ControlledDocument_Approval", "Status <> 2 OR (ApprovedBy IS NOT NULL AND ApprovedAtUtc IS NOT NULL AND EffectiveDate IS NOT NULL AND ApprovalReason IS NOT NULL)");
        });
        migrationBuilder.CreateIndex("IX_ControlledDocuments_DocumentCode_Version", "ControlledDocuments", new[] { "DocumentCode", "Version" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ControlledDocuments");
}
