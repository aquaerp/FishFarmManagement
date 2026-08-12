using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FishFarmManager.Tests;

public sealed class Stage4MigrationTests
{
    [Fact]
    public void EmptyDatabase_MigratesToLatestAndPassesIntegrityValidation()
    {
        WithDatabase((context, _) =>
        {
            context.Database.Migrate();
            Assert.Empty(context.Database.GetPendingMigrations());
            StartupValidationService.ValidateDatabase(context);
        });
    }

    [Fact]
    public void InitialSchema_MigratesToLatestWithoutLosingExistingData()
    {
        WithDatabase((context, path) =>
        {
            context.GetService<IMigrator>().Migrate("20251014162958_InitialCreate");
            context.Customers.Add(new Customer
            {
                Name = "Migration Survivor", Type = CustomerType.Individual,
                Status = CustomerStatus.Active, CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            context.Database.Migrate();

            using var verification = CreateContext(path);
            Assert.Equal("Migration Survivor", verification.Customers.Single().Name);
            Assert.Empty(verification.Database.GetPendingMigrations());
            StartupValidationService.ValidateDatabase(verification);
        });
    }

    private static void WithDatabase(Action<FishFarmContext, string> test)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aquafarm-g4-migration-{Guid.NewGuid():N}.db");
        try { using var context = CreateContext(path); test(context, path); }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    private static FishFarmContext CreateContext(string path) => new(
        new DbContextOptionsBuilder<FishFarmContext>().UseSqlite($"Data Source={path};Pooling=False;Foreign Keys=True").Options);
}

public sealed class Stage4BackupFailureTests
{
    [Fact]
    public async Task TamperedBackup_IsRejectedAndLiveDatabaseRemainsUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aquafarm-g4-backup-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(root, "data.db");
        var backupDirectory = Path.Combine(root, "backups");
        Directory.CreateDirectory(root);
        var configuration = RuntimePathsTests.BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath};Pooling=False",
            ["Database:Provider"] = "SQLite", ["Backup:Directory"] = backupDirectory,
            ["Backup:Encrypt"] = "true", ["Backup:MaxBackups"] = "10"
        });

        try
        {
            var options = new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite(RuntimePaths.ResolveConnectionString(configuration)).Options;
            await using var context = new FishFarmContext(options);
            await context.Database.MigrateAsync();
            context.Customers.Add(new Customer
            {
                Name = "Live Data", Type = CustomerType.Individual,
                Status = CustomerStatus.Active, CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            var service = new BackupService(context, configuration);
            Assert.True(await service.CreateBackup());
            var backup = Assert.Single(service.GetAvailableBackups());
            var bytes = await File.ReadAllBytesAsync(backup.FullPath);
            bytes[^1] ^= 0xFF;
            await File.WriteAllBytesAsync(backup.FullPath, bytes);

            Assert.False(await service.RestoreBackup(backup.FullPath));
            await using var verification = new FishFarmContext(options);
            Assert.Equal("Live Data", (await verification.Customers.SingleAsync()).Name);
            StartupValidationService.ValidateDatabase(verification);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
