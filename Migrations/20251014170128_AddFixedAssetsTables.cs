using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedAssetsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FixedAssetId",
                table: "MaintenanceRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssetNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResidualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsefulLifeYears = table.Column<int>(type: "INTEGER", nullable: false),
                    DepreciationMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualDepreciationRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BookValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResponsibleEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ManufactureYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InServiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OutOfServiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisposalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisposalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WarrantyMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresInsurance = table.Column<bool>(type: "INTEGER", nullable: false),
                    InsurancePolicyNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InsuranceExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresMaintenance = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaintenanceFrequencyMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Employees_ResponsibleEmployeeId",
                        column: x => x.ResponsibleEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FixedAssets_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetDepreciations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FixedAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    DepreciationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpeningBookValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepreciationAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingBookValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DaysInUse = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyDepreciation = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsAutoCalculated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CalculatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDepreciations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetDepreciations_FixedAssets_FixedAssetId",
                        column: x => x.FixedAssetId,
                        principalTable: "FixedAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_FixedAssetId",
                table: "MaintenanceRecords",
                column: "FixedAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDepreciations_FixedAssetId",
                table: "AssetDepreciations",
                column: "FixedAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_ResponsibleEmployeeId",
                table: "FixedAssets",
                column: "ResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_SupplierId",
                table: "FixedAssets",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRecords_FixedAssets_FixedAssetId",
                table: "MaintenanceRecords",
                column: "FixedAssetId",
                principalTable: "FixedAssets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRecords_FixedAssets_FixedAssetId",
                table: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "AssetDepreciations");

            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_FixedAssetId",
                table: "MaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "FixedAssetId",
                table: "MaintenanceRecords");
        }
    }
}
