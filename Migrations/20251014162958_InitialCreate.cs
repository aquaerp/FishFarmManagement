using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TaxNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CommercialRegistration = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NationalId = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HireDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Position = table.Column<int>(type: "INTEGER", maxLength: 100, nullable: false),
                    Department = table.Column<int>(type: "INTEGER", maxLength: 100, nullable: false),
                    EmploymentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    BasicSalary = table.Column<decimal>(type: "TEXT", nullable: false),
                    HousingAllowance = table.Column<decimal>(type: "TEXT", nullable: true),
                    TransportationAllowance = table.Column<decimal>(type: "TEXT", nullable: true),
                    FoodAllowance = table.Column<decimal>(type: "TEXT", nullable: true),
                    OtherAllowances = table.Column<decimal>(type: "TEXT", nullable: true),
                    SocialInsuranceEnrolled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SocialInsuranceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SocialInsurancePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    HealthInsuranceEnrolled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnnualLeaveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedLeaveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    SickLeaveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IBAN = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ponds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Capacity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Area = table.Column<decimal>(type: "TEXT", nullable: false),
                    Depth = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    PondType = table.Column<int>(type: "INTEGER", nullable: false),
                    AerationSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    FiltrationSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    HeatingSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    CoolingSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    FishCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    OptimalWaterTemperature = table.Column<decimal>(type: "TEXT", nullable: true),
                    OptimalWaterPH = table.Column<decimal>(type: "TEXT", nullable: true),
                    OptimalOxygenLevel = table.Column<decimal>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ponds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    CycleType = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialFishCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InitialAverageWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    FrySource = table.Column<string>(type: "TEXT", nullable: true),
                    HatcherySource = table.Column<string>(type: "TEXT", nullable: true),
                    ExpectedHatchDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalFishCount = table.Column<int>(type: "INTEGER", nullable: true),
                    FinalAverageWeight = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalHarvestWeight = table.Column<decimal>(type: "TEXT", nullable: true),
                    SurvivalRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    FCR = table.Column<decimal>(type: "TEXT", nullable: true),
                    ADG = table.Column<decimal>(type: "TEXT", nullable: true),
                    ActualLarvalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Fax = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TaxNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CommercialRegister = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VATConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaxRegistrationNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CompanyNameEN = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DefaultVATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EnableEInvoicing = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableZATCAIntegration = table.Column<bool>(type: "INTEGER", nullable: false),
                    ZATCAApiEndpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ZATCAApiKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VATRegistrationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnSubmissionDay = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VATConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    VATAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeliveryInstructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomerId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId1",
                        column: x => x.CustomerId1,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HoursWorked = table.Column<decimal>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WorkedHours = table.Column<decimal>(type: "TEXT", nullable: true),
                    LateMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    EarlyLeaveMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    OvertimeHours = table.Column<decimal>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendances_Employees_EmployeeId1",
                        column: x => x.EmployeeId1,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLeaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaveNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LeaveType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DaysCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaves_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Salaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SalaryNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    PayPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "TEXT", nullable: false),
                    HousingAllowance = table.Column<decimal>(type: "TEXT", nullable: false),
                    TransportationAllowance = table.Column<decimal>(type: "TEXT", nullable: false),
                    FoodAllowance = table.Column<decimal>(type: "TEXT", nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "TEXT", nullable: false),
                    WorkDays = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedWorkDays = table.Column<int>(type: "INTEGER", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    OvertimeRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalBonuses = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCommissions = table.Column<decimal>(type: "TEXT", nullable: false),
                    AbsenceDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AbsenceDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    LateDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LateDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    SocialInsuranceDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    HealthInsuranceDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalOtherDeductions = table.Column<decimal>(type: "TEXT", nullable: false),
                    Loan = table.Column<decimal>(type: "TEXT", nullable: false),
                    Advance = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrossSalary = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "TEXT", nullable: false),
                    NetSalary = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PreparedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Salaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HireDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Training = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Certificate = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EquipmentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Supplier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WarrantyPeriod = table.Column<int>(type: "INTEGER", nullable: false),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstallationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OperatingHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    LastMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaintenanceIntervalDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AnnualMaintenanceCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Specifications = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SafetyNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HACCPRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HazardType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ControlPoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ControlPointDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HazardDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Likelihood = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitoringMethod = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    MeasurementUnit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MinimumLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaximumLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    TargetValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ActualValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    MeasurementTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsWithinLimits = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviationOccurred = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviationDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CorrectiveActions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ActionTakenDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActionTakenBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VerificationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReferenceDocument = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EquipmentUsed = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RiskLevel = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HACCPRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HACCPRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatchRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchType = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RelatedCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchRecords_ProductionCycles_RelatedCycleId",
                        column: x => x.RelatedCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Certifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CertificateName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuingAuthority = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CertificateNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AttachmentPath = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RenewalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidityPeriodDays = table.Column<int>(type: "INTEGER", nullable: true),
                    CertificationFee = table.Column<decimal>(type: "TEXT", nullable: true),
                    CertificationCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    RenewalCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    AnnualMaintenanceCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    LastAuditDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextAuditDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAuditResult = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AuditScore = table.Column<int>(type: "INTEGER", nullable: true),
                    MajorNonConformities = table.Column<int>(type: "INTEGER", nullable: true),
                    MinorNonConformities = table.Column<int>(type: "INTEGER", nullable: true),
                    CriticalNonConformities = table.Column<int>(type: "INTEGER", nullable: true),
                    ApplicableProducts = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CorrectiveActionsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    CorrectiveActionsPlan = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CorrectiveActionsDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CorrectiveActionsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CorrectiveActionsCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RenewalApplicationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RenewalInspectionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RenewalInProgress = table.Column<bool>(type: "INTEGER", nullable: false),
                    RenewalStatus = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CertificateFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AuditReportFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResponsiblePerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AuditorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AuditorOrganization = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AuthorityWebsite = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AuthorityCountry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StandardVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ComplianceRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NonConformityDetails = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certifications_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentalRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Temperature = table.Column<decimal>(type: "TEXT", nullable: false),
                    Humidity = table.Column<decimal>(type: "TEXT", nullable: false),
                    WindSpeed = table.Column<decimal>(type: "TEXT", nullable: false),
                    WeatherCondition = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Parameter = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Value = table.Column<decimal>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentalRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnvironmentalRecords_ProductionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeedingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeedingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FeedType = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<double>(type: "REAL", nullable: false),
                    FeedPrice = table.Column<double>(type: "REAL", nullable: false),
                    FeedingTimes = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedFishWeight = table.Column<double>(type: "REAL", nullable: true),
                    EstimatedFishCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedingRecords_ProductionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FishHealthRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HealthStatus = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Symptoms = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Diagnosis = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Treatment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Disease = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ActionTaken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishHealthRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FishHealthRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FishHealthRecords_ProductionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    InspectionNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    InspectorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InspectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    SamplePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    HealthyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SickCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DeadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BodyConditionScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    HasSkinLesions = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasFinDamage = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasEyeProblems = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasGillProblems = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasBloating = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDiscoloration = table.Column<bool>(type: "INTEGER", nullable: false),
                    NormalSwimming = table.Column<bool>(type: "INTEGER", nullable: false),
                    NormalFeeding = table.Column<bool>(type: "INTEGER", nullable: false),
                    Lethargy = table.Column<bool>(type: "INTEGER", nullable: false),
                    AbnormalGathering = table.Column<bool>(type: "INTEGER", nullable: false),
                    SurfaceGasping = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParasiteDetection = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParasiteTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BacterialInfection = table.Column<bool>(type: "INTEGER", nullable: false),
                    BacteriaTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ViralInfection = table.Column<bool>(type: "INTEGER", nullable: false),
                    VirusTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FungalInfection = table.Column<bool>(type: "INTEGER", nullable: false),
                    FungusTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PrimaryDiagnosis = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SecondaryDiagnosis = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OverallHealthStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    MortalityRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TreatmentRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecommendedTreatment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsolationRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    CullingRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreventiveMeasures = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FollowUpNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    VeterinarianName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LaboratoryName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GeneralObservations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthInspections_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthInspections_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MortalityRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeadFishCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageWeight = table.Column<double>(type: "REAL", nullable: true),
                    Cause = table.Column<int>(type: "INTEGER", nullable: false),
                    CauseDescription = table.Column<string>(type: "TEXT", nullable: false),
                    ActionTaken = table.Column<string>(type: "TEXT", nullable: false),
                    Treatment = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MortalityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MortalityRecords_ProductionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCyclePonds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PondId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCyclePonds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionCyclePonds_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionCyclePonds_Ponds_PondId1",
                        column: x => x.PondId1,
                        principalTable: "Ponds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionCyclePonds_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    TestType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TestName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SampleType = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Result = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AverageWeight = table.Column<decimal>(type: "TEXT", nullable: true),
                    AverageLength = table.Column<decimal>(type: "TEXT", nullable: true),
                    UniformityPercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    Appearance = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Moisture = table.Column<decimal>(type: "TEXT", nullable: true),
                    Protein = table.Column<decimal>(type: "TEXT", nullable: true),
                    Fat = table.Column<decimal>(type: "TEXT", nullable: true),
                    Ash = table.Column<decimal>(type: "TEXT", nullable: true),
                    pH = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalBacteriaCount = table.Column<decimal>(type: "TEXT", nullable: true),
                    ColiformCount = table.Column<decimal>(type: "TEXT", nullable: true),
                    SalmonellaPresence = table.Column<bool>(type: "INTEGER", nullable: true),
                    EColiPresence = table.Column<bool>(type: "INTEGER", nullable: true),
                    ColorScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    OdorScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    TextureScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    TasteScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    Mercury = table.Column<decimal>(type: "TEXT", nullable: true),
                    Lead = table.Column<decimal>(type: "TEXT", nullable: true),
                    Cadmium = table.Column<decimal>(type: "TEXT", nullable: true),
                    Arsenic = table.Column<decimal>(type: "TEXT", nullable: true),
                    TestResult = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    MeetsStandards = table.Column<bool>(type: "INTEGER", nullable: true),
                    StandardsReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TestedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Laboratory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CertificateNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityTests_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityTests_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TreatmentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    TreatmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TreatmentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TreatmentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Dosage = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WithdrawalPeriod = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreatmentRecords_ProductionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WaterQualityRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Temperature = table.Column<decimal>(type: "TEXT", nullable: false),
                    pH = table.Column<decimal>(type: "TEXT", nullable: false),
                    DissolvedOxygen = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ammonia = table.Column<decimal>(type: "TEXT", nullable: false),
                    Nitrite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Nitrate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Salinity = table.Column<decimal>(type: "TEXT", nullable: true),
                    Turbidity = table.Column<decimal>(type: "TEXT", nullable: true),
                    Alkalinity = table.Column<decimal>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    MeasurementDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    PondId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterQualityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterQualityRecords_Employees_RecordedByEmployeeId",
                        column: x => x.RecordedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterQualityRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WaterQualityRecords_Ponds_PondId1",
                        column: x => x.PondId1,
                        principalTable: "Ponds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaterQualityRecords_ProductionCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CostRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CostType = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DocumentNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentReference = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    PaymentDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRecords_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRecords_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ItemCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Category = table.Column<int>(type: "INTEGER", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RequiredTemperature = table.Column<decimal>(type: "TEXT", nullable: true),
                    RequiredHumidity = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MaximumStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    ReorderQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastPurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LastPurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StorageLocation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BatchNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualDeliveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountAfterDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VATAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PaymentDueDays = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsReceived = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPartiallyReceived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReceivedPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RequestedById = table.Column<int>(type: "INTEGER", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentById = table.Column<int>(type: "INTEGER", nullable: true),
                    SentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReceivedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Employees_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Employees_ReceivedById",
                        column: x => x.ReceivedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Employees_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Employees_SentById",
                        column: x => x.SentById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PaidBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPayments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SalesOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    PaymentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReceivedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SalesOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SupplyDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InvoiceType = table.Column<int>(type: "INTEGER", nullable: false),
                    SellerVATNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    SellerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SellerAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerVATNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    BuyerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BuyerAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VATAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWithVAT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QRCodeContent = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    QRCodeImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UUID = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PIH = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InvoiceHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsSubmittedToZATCA = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmittedToZATCADate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ZATCAResponseCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ZATCAResponseMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SalesOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxInvoices_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VATReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PeriodStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Box1_TaxableSalesInKSA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box2_ZeroRatedSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box3_Exports = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box4_ExemptSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box5_TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box6_VATOnSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box7_TotalPurchases = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box8_GCCPurchases = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box9_TaxableImports = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box10_VATOnPurchases = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box11_NetVATDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box12_Adjustments = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box13_TotalVATDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box14_RecoverablePreviousPeriod = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Box15_NetVATDueForPeriod = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VATReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VATReturns_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VATReturns_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaintenanceType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProblemDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    WorkPerformed = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PartsReplaced = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PartsCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    LaborCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Technician = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequiresFollowUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TotalCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PondId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Ponds_PondId",
                        column: x => x.PondId,
                        principalTable: "Ponds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaintenanceType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", nullable: false),
                    FrequencyDays = table.Column<int>(type: "INTEGER", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    AssignedTo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequiredParts = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MaintenanceInstructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SendNotification = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationDaysBefore = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpareParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PartNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    StockQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumStock = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantityInStock = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Supplier = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastPurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCritical = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpareParts_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CertificationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CertificationId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CertificateName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CertificateNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificationRecords_Certifications_CertificationId",
                        column: x => x.CertificationId,
                        principalTable: "Certifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryValuations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarketPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetRealizableValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WriteDownAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryValuations_Employees_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryValuations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryValuations_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReferenceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductionCycleId = table.Column<int>(type: "INTEGER", nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnitCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRejected = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedById = table.Column<int>(type: "INTEGER", nullable: true),
                    InventoryItemId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_Employees_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryItems_InventoryItemId1",
                        column: x => x.InventoryItemId1,
                        principalTable: "InventoryItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_ProductionCycles_ProductionCycleId",
                        column: x => x.ProductionCycleId,
                        principalTable: "ProductionCycles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BatchNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    QualityAccepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualityResult = table.Column<int>(type: "INTEGER", nullable: true),
                    QualityNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceivings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceivingNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsFullReceiving = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPartialReceiving = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverallQualityResult = table.Column<int>(type: "INTEGER", nullable: false),
                    QualityInspectionCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    InspectedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InspectionNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReceivingTemperature = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PackagingCondition = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    HasDamage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DamageDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SupplierInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SupplierInvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupplierInvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GoodsReceivedNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReceivedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceivings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReceivings_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaxInvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VATAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxInvoiceItems_TaxInvoices_TaxInvoiceId",
                        column: x => x.TaxInvoiceId,
                        principalTable: "TaxInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceivingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseReceivingId = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchaseOrderItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BatchNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    QualityAccepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualityResult = table.Column<int>(type: "INTEGER", nullable: true),
                    QualityNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AddedToStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    StockAddedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StockMovementId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceivingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReceivingItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReceivingItems_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReceivingItems_PurchaseReceivings_PurchaseReceivingId",
                        column: x => x.PurchaseReceivingId,
                        principalTable: "PurchaseReceivings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReceivingItems_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId",
                table: "Attendances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId1",
                table: "Attendances",
                column: "EmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_BatchRecords_RelatedCycleId",
                table: "BatchRecords",
                column: "RelatedCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRecords_CertificationId",
                table: "CertificationRecords",
                column: "CertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Certifications_ProductionCycleId",
                table: "Certifications",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRecords_PondId",
                table: "CostRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRecords_ProductionCycleId",
                table: "CostRecords",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRecords_SupplierId",
                table: "CostRecords",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_CustomerId",
                table: "CustomerPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_SalesOrderId",
                table: "CustomerPayments",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaves_EmployeeId",
                table: "EmployeeLeaves",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalRecords_CycleId",
                table: "EnvironmentalRecords",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalRecords_PondId",
                table: "EnvironmentalRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_PondId",
                table: "Equipment",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedingRecords_CycleId",
                table: "FeedingRecords",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_FishHealthRecords_CycleId",
                table: "FishHealthRecords",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_FishHealthRecords_PondId",
                table: "FishHealthRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_HACCPRecords_PondId",
                table: "HACCPRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInspections_PondId",
                table: "HealthInspections",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInspections_ProductionCycleId",
                table: "HealthInspections",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SupplierId",
                table: "InventoryItems",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuations_ApprovedById",
                table: "InventoryValuations",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuations_EmployeeId",
                table: "InventoryValuations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuations_InventoryItemId",
                table: "InventoryValuations",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_EquipmentId",
                table: "MaintenanceRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_PondId",
                table: "MaintenanceRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_EquipmentId",
                table: "MaintenanceSchedules",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MortalityRecords_CycleId",
                table: "MortalityRecords",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCyclePonds_PondId",
                table: "ProductionCyclePonds",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCyclePonds_PondId1",
                table: "ProductionCyclePonds",
                column: "PondId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCyclePonds_ProductionCycleId",
                table: "ProductionCyclePonds",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_InventoryItemId",
                table: "PurchaseOrderItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ApprovedById",
                table: "PurchaseOrders",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ReceivedById",
                table: "PurchaseOrders",
                column: "ReceivedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_RequestedById",
                table: "PurchaseOrders",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SentById",
                table: "PurchaseOrders",
                column: "SentById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceivingItems_InventoryItemId",
                table: "PurchaseReceivingItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceivingItems_PurchaseOrderItemId",
                table: "PurchaseReceivingItems",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceivingItems_PurchaseReceivingId",
                table: "PurchaseReceivingItems",
                column: "PurchaseReceivingId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceivingItems_StockMovementId",
                table: "PurchaseReceivingItems",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceivings_PurchaseOrderId",
                table: "PurchaseReceivings",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityTests_PondId",
                table: "QualityTests",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityTests_ProductionCycleId",
                table: "QualityTests",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_EmployeeId",
                table: "Salaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_ProductionCycleId",
                table: "SalesOrderItems",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SalesOrderId",
                table: "SalesOrderItems",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId1",
                table: "SalesOrders",
                column: "CustomerId1");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_EquipmentId",
                table: "SpareParts",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffRecords_EmployeeId",
                table: "StaffRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ApprovedById",
                table: "StockMovements",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CustomerId",
                table: "StockMovements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_EmployeeId",
                table: "StockMovements",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId",
                table: "StockMovements",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId1",
                table: "StockMovements",
                column: "InventoryItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductionCycleId",
                table: "StockMovements",
                column: "ProductionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SupplierId",
                table: "StockMovements",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxInvoiceItems_TaxInvoiceId",
                table: "TaxInvoiceItems",
                column: "TaxInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxInvoices_CustomerId",
                table: "TaxInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxInvoices_SalesOrderId",
                table: "TaxInvoices",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRecords_CycleId",
                table: "TreatmentRecords",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRecords_PondId",
                table: "TreatmentRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VATReturns_CreatedById",
                table: "VATReturns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VATReturns_ModifiedById",
                table: "VATReturns",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityRecords_CycleId",
                table: "WaterQualityRecords",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityRecords_PondId",
                table: "WaterQualityRecords",
                column: "PondId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityRecords_PondId1",
                table: "WaterQualityRecords",
                column: "PondId1");

            migrationBuilder.CreateIndex(
                name: "IX_WaterQualityRecords_RecordedByEmployeeId",
                table: "WaterQualityRecords",
                column: "RecordedByEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "BatchRecords");

            migrationBuilder.DropTable(
                name: "CertificationRecords");

            migrationBuilder.DropTable(
                name: "CostRecords");

            migrationBuilder.DropTable(
                name: "CustomerPayments");

            migrationBuilder.DropTable(
                name: "EmployeeLeaves");

            migrationBuilder.DropTable(
                name: "EnvironmentalRecords");

            migrationBuilder.DropTable(
                name: "FeedingRecords");

            migrationBuilder.DropTable(
                name: "FishHealthRecords");

            migrationBuilder.DropTable(
                name: "HACCPRecords");

            migrationBuilder.DropTable(
                name: "HealthInspections");

            migrationBuilder.DropTable(
                name: "InventoryValuations");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules");

            migrationBuilder.DropTable(
                name: "MortalityRecords");

            migrationBuilder.DropTable(
                name: "ProductionCyclePonds");

            migrationBuilder.DropTable(
                name: "PurchaseReceivingItems");

            migrationBuilder.DropTable(
                name: "QualityTests");

            migrationBuilder.DropTable(
                name: "Salaries");

            migrationBuilder.DropTable(
                name: "SalesOrderItems");

            migrationBuilder.DropTable(
                name: "SpareParts");

            migrationBuilder.DropTable(
                name: "StaffRecords");

            migrationBuilder.DropTable(
                name: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "TaxInvoiceItems");

            migrationBuilder.DropTable(
                name: "TreatmentRecords");

            migrationBuilder.DropTable(
                name: "VATConfigurations");

            migrationBuilder.DropTable(
                name: "VATReturns");

            migrationBuilder.DropTable(
                name: "WaterQualityRecords");

            migrationBuilder.DropTable(
                name: "Certifications");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "PurchaseReceivings");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "TaxInvoices");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "ProductionCycles");

            migrationBuilder.DropTable(
                name: "Ponds");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
