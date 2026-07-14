using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260714000100_AddPersistentSecurityState")]
public sealed class AddPersistentSecurityState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("FailedLoginCount", "Users", "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>("LastFailedLoginAt", "Users", "TEXT", nullable: true);
        migrationBuilder.AddColumn<DateTime>("LockoutEnd", "Users", "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>("MustChangePassword", "Users", "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>("PasswordChangedAt", "Users", "TEXT", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("FailedLoginCount", "Users");
        migrationBuilder.DropColumn("LastFailedLoginAt", "Users");
        migrationBuilder.DropColumn("LockoutEnd", "Users");
        migrationBuilder.DropColumn("MustChangePassword", "Users");
        migrationBuilder.DropColumn("PasswordChangedAt", "Users");
    }
}
