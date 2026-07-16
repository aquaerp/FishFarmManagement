using System;
using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716080000_AddCorrectiveActions")]
public sealed class AddCorrectiveActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("CorrectiveActions", table => new
        {
            Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true), ReferenceNumber = table.Column<string>("TEXT", maxLength: 50, nullable: false),
            Nonconformity = table.Column<string>("TEXT", maxLength: 1000, nullable: false), RootCause = table.Column<string>("TEXT", maxLength: 1000, nullable: false),
            ActionPlan = table.Column<string>("TEXT", maxLength: 1000, nullable: false), Owner = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            DueDate = table.Column<DateTime>("TEXT", nullable: false), Status = table.Column<int>("INTEGER", nullable: false), CompletedAtUtc = table.Column<DateTime>("TEXT", nullable: true),
            CompletedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true), EffectivenessVerified = table.Column<bool>("INTEGER", nullable: false),
            VerifiedBy = table.Column<string>("TEXT", maxLength: 100, nullable: true), VerifiedAtUtc = table.Column<DateTime>("TEXT", nullable: true), VerificationNotes = table.Column<string>("TEXT", maxLength: 1000, nullable: true),
            CreatedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false), CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_CorrectiveActions", x => x.Id); table.CheckConstraint("CK_CAPA_ClosedVerification", "Status <> 4 OR (EffectivenessVerified = 1 AND VerifiedBy IS NOT NULL AND VerifiedAtUtc IS NOT NULL AND CompletedAtUtc IS NOT NULL)"); });
        migrationBuilder.CreateIndex("IX_CorrectiveActions_ReferenceNumber", "CorrectiveActions", "ReferenceNumber", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("CorrectiveActions");
}
