using System.Text;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FishFarmManager.Tests;

public sealed class RuntimePathsTests
{
    [Fact]
    public void ResolveDatabasePath_ExpandsEnvironmentVariablesAndReturnsAbsolutePath()
    {
        var expected = Path.Combine(Path.GetTempPath(), $"aquafarm-path-{Guid.NewGuid():N}.db");
        Environment.SetEnvironmentVariable("AQUAFARM_TEST_DB", expected);
        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=%AQUAFARM_TEST_DB%",
                ["Database:Provider"] = "SQLite"
            });

            Assert.Equal(Path.GetFullPath(expected), RuntimePaths.ResolveDatabasePath(configuration));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AQUAFARM_TEST_DB", null);
        }
    }

    [Fact]
    public void StartupValidation_RejectsDemoDataInProduction()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aquafarm-startup-{Guid.NewGuid():N}");
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(root, "data.db")}",
            ["Database:Provider"] = "SQLite",
            ["AppSettings:Environment"] = "Production",
            ["AppSettings:SeedDemoData"] = "true",
            ["Backup:Encrypt"] = "true"
        });

        Assert.Throws<InvalidOperationException>(() => StartupValidationService.ValidateAndPrepare(configuration));
    }

    internal static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}

public sealed class BackupServiceTests
{
    [Fact]
    public async Task EncryptedBackup_CanBeRestoredThreeConsecutiveTimes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aquafarm-backup-test-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(root, "data", "FishFarm.db");
        var backupDirectory = Path.Combine(root, "backups");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var configuration = RuntimePathsTests.BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath};Pooling=False",
            ["Database:Provider"] = "SQLite",
            ["AppSettings:Environment"] = "Production",
            ["AppSettings:SeedDemoData"] = "false",
            ["Backup:Directory"] = backupDirectory,
            ["Backup:Encrypt"] = "true",
            ["Backup:MaxBackups"] = "20"
        });

        try
        {
            var options = new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite(RuntimePaths.ResolveConnectionString(configuration))
                .Options;

            await using (var context = new FishFarmContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                context.Customers.Add(new Customer
                {
                    Name = "Original Customer",
                    Type = CustomerType.Individual,
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();

                var service = new BackupService(context, configuration);
                Assert.True(await service.CreateBackup());
                var backup = Assert.Single(service.GetAvailableBackups(), item => item.Type == BackupType.Manual);
                Assert.True(backup.IsEncrypted);
                Assert.False(File.ReadAllBytes(backup.FullPath).AsSpan().StartsWith(Encoding.ASCII.GetBytes("SQLite format 3")));

                context.Customers.Add(new Customer
                {
                    Name = "Temporary Customer",
                    Type = CustomerType.Individual,
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    Assert.True(await service.RestoreBackup(backup.FullPath));
                    await using var verification = new FishFarmContext(options);
                    Assert.Equal(1, await verification.Customers.CountAsync());
                }
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}

public sealed class AuthenticationPersistenceTests
{
    [Fact]
    public void AccountLockout_PersistsAcrossServiceInstances()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"aquafarm-auth-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<FishFarmContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            const string username = "persistent_lock_user";
            const string password = "Valid!Password123";
            using (var context = new FishFarmContext(options))
            {
                context.Database.EnsureCreated();
                context.Users.Add(new User
                {
                    Username = username,
                    FullName = "Persistent Lock User",
                    Role = UserRole.Viewer,
                    IsActive = true,
                    PasswordHash = AuthenticationService.HashPassword(password),
                    PasswordChangedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Test"
                });
                context.SaveChanges();

                var authentication = new AuthenticationService(context);
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    Assert.False(authentication.Login(username, "Wrong!Password123"));
                }
            }

            using (var context = new FishFarmContext(options))
            {
                var persisted = context.Users.Single(user => user.Username == username);
                Assert.True(persisted.LockoutEnd > DateTime.UtcNow);
                Assert.Equal(5, persisted.FailedLoginCount);
                Assert.False(new AuthenticationService(context).Login(username, password));

                persisted.LockoutEnd = DateTime.UtcNow.AddMinutes(-1);
                context.SaveChanges();
                Assert.True(new AuthenticationService(context).Login(username, password));
            }
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public void Migrations_AddPersistentSecurityColumns()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"aquafarm-migration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<FishFarmContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;
        try
        {
            using var context = new FishFarmContext(options);
            context.Database.Migrate();
            using var command = context.Database.GetDbConnection().CreateCommand();
            context.Database.OpenConnection();
            command.CommandText = "PRAGMA table_info('Users');";
            using var reader = command.ExecuteReader();
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read()) columns.Add(reader.GetString(1));

            Assert.Contains("FailedLoginCount", columns);
            Assert.Contains("LockoutEnd", columns);
            Assert.Contains("MustChangePassword", columns);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }
}
