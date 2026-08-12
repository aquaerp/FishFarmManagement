using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;

namespace FishFarmManager.Services;

public sealed record StartupValidationResult(
    string DatabasePath,
    string BackupDirectory,
    IReadOnlyList<string> Warnings);

public static class StartupValidationService
{
    private const long MinimumFreeBytes = 100L * 1024L * 1024L;

    public static StartupValidationResult ValidateAndPrepare(IConfiguration configuration)
    {
        var warnings = new List<string>();
        var provider = configuration["Database:Provider"];
        if (!string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Database:Provider must be SQLite for the approved single-station pilot.");
        }

        var environment = configuration["AppSettings:Environment"] ?? "Production";
        var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
        if (isProduction && configuration.GetValue("AppSettings:SeedDemoData", false))
        {
            throw new InvalidOperationException("Demo data seeding is forbidden in Production.");
        }

        if (isProduction && !configuration.GetValue("Backup:Encrypt", true))
        {
            throw new InvalidOperationException("Encrypted backups are required in Production.");
        }

        var databasePath = RuntimePaths.ResolveDatabasePath(configuration);
        var databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The database directory could not be resolved.");
        EnsureWritableDirectory(databaseDirectory, "database");

        var backupDirectory = RuntimePaths.ResolveBackupDirectory(configuration);
        EnsureWritableDirectory(backupDirectory, "backup");

        var root = Path.GetPathRoot(databasePath);
        if (!string.IsNullOrWhiteSpace(root))
        {
            try
            {
                var drive = new DriveInfo(root);
                if (drive.IsReady && drive.AvailableFreeSpace < MinimumFreeBytes)
                {
                    throw new InvalidOperationException("Insufficient free space: at least 100 MB is required.");
                }
            }
            catch (ArgumentException)
            {
                warnings.Add("Free disk space could not be determined for the database path.");
            }
        }

        if (configuration.GetValue("Backup:ExternalCopyRequired", false))
        {
            var configuredExternalDirectory = configuration["Backup:ExternalDirectory"];
            if (string.IsNullOrWhiteSpace(configuredExternalDirectory))
            {
                throw new InvalidOperationException("Backup:ExternalDirectory is required when external copies are mandatory.");
            }

            var externalDirectory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(configuredExternalDirectory));
            var externalRoot = Path.GetPathRoot(externalDirectory);
            if (string.IsNullOrWhiteSpace(externalRoot)
                || !Directory.Exists(externalRoot)
                || !new DriveInfo(externalRoot).IsReady)
            {
                throw new InvalidOperationException("The required external backup drive is not ready.");
            }

            EnsureWritableDirectory(externalDirectory, "external-backup");
        }

        return new StartupValidationResult(databasePath, backupDirectory, warnings);
    }

    public static void ValidateDatabase(FishFarmContext context)
    {
        var pendingMigrations = context.Database.GetPendingMigrations().ToArray();
        if (pendingMigrations.Length != 0)
        {
            throw new InvalidOperationException(
                $"Database schema is not current. Pending migrations: {string.Join(", ", pendingMigrations)}");
        }

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(command.ExecuteScalar());
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Database integrity validation failed.");
            }

            command.CommandText = "PRAGMA foreign_key_check;";
            using var violations = command.ExecuteReader();
            if (violations.Read())
            {
                throw new InvalidOperationException("Database foreign-key validation failed.");
            }
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static void EnsureWritableDirectory(string path, string purpose)
    {
        Directory.CreateDirectory(path);
        var probe = Path.Combine(path, $".aquafarm-{purpose}-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(probe, "probe");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException($"The {purpose} directory is not writable: {path}", ex);
        }
        finally
        {
            if (File.Exists(probe))
            {
                File.Delete(probe);
            }
        }
    }
}
