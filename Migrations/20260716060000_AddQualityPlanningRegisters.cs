using System;
using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716060000_AddQualityPlanningRegisters")]
public sealed class AddQualityPlanningRegisters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QualityObjectives",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ObjectiveCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Measure = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                BaselineValue = table.Column<decimal>(type: "TEXT", nullable: false),
                TargetValue = table.Column<decimal>(type: "TEXT", nullable: false),
                CurrentValue = table.Column<decimal>(type: "TEXT", nullable: false),
                Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Owner = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                LastMeasuredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QualityObjectives", x => x.Id);
                table.CheckConstraint("CK_QualityObjective_Period", "PeriodStart <= PeriodEnd");
            });

        migrationBuilder.CreateTable(
            name: "QualityRiskRegisters",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Owner = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Likelihood = table.Column<int>(type: "INTEGER", nullable: false),
                Impact = table.Column<int>(type: "INTEGER", nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: false),
                TreatmentPlan = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                EffectivenessReviewed = table.Column<bool>(type: "INTEGER", nullable: false),
                ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                ReviewNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QualityRiskRegisters", x => x.Id);
                table.CheckConstraint("CK_QualityRiskRegister_Score", "Likelihood BETWEEN 1 AND 5 AND Impact BETWEEN 1 AND 5 AND Score = Likelihood * Impact");
            });

        migrationBuilder.CreateIndex(name: "IX_QualityObjectives_ObjectiveCode", table: "QualityObjectives", column: "ObjectiveCode", unique: true);
        migrationBuilder.CreateIndex(name: "IX_QualityRiskRegisters_ReferenceNumber", table: "QualityRiskRegisters", column: "ReferenceNumber", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QualityObjectives");
        migrationBuilder.DropTable(name: "QualityRiskRegisters");
    }
}
