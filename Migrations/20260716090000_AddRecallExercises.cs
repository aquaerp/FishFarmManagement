using System;
using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716090000_AddRecallExercises")]
public sealed class AddRecallExercises : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("RecallExercises", table => new
        {
            Id = table.Column<int>("INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true), ExerciseNumber = table.Column<string>("TEXT", maxLength: 50, nullable: false), TriggerLotCode = table.Column<string>("TEXT", maxLength: 100, nullable: false),
            StartedAtUtc = table.Column<DateTime>("TEXT", nullable: false), CompletedAtUtc = table.Column<DateTime>("TEXT", nullable: false), AffectedOrderCount = table.Column<int>("INTEGER", nullable: false), AffectedCustomerCount = table.Column<int>("INTEGER", nullable: false), AffectedHarvestLotCount = table.Column<int>("INTEGER", nullable: false), PassedTwoHourTarget = table.Column<bool>("INTEGER", nullable: false), ConductedBy = table.Column<string>("TEXT", maxLength: 100, nullable: false), Findings = table.Column<string>("TEXT", maxLength: 2000, nullable: true), CreatedAtUtc = table.Column<DateTime>("TEXT", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_RecallExercises", x => x.Id); table.CheckConstraint("CK_RecallExercise_Timeline", "CompletedAtUtc >= StartedAtUtc"); });
        migrationBuilder.CreateIndex("IX_RecallExercises_ExerciseNumber", "RecallExercises", "ExerciseNumber", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("RecallExercises");
}
