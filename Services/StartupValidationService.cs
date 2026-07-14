using Microsoft.Extensions.Configuration;

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

        if (configuration.GetValue("Backup:ExternalCopyRequired", false)
            && string.IsNullOrWhiteSpace(configuration["Backup:ExternalDirectory"]))
        {
            throw new InvalidOperationException("Backup:ExternalDirectory is required when external copies are mandatory.");
        }

        return new StartupValidationResult(databasePath, backupDirectory, warnings);
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

