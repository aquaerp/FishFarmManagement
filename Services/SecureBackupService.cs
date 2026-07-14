using System.Data;
using System.Security.Cryptography;
using FishFarmManager.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FishFarmManager.Services;

public sealed class BackupService
{
    private static readonly byte[] BackupEntropy = "AquaFarmPro.Backup.v1"u8.ToArray();
    private static string? _lastKnownBackupDirectory;
    private readonly FishFarmContext _context;
    private readonly string _databasePath;
    private readonly string _backupDirectory;
    private readonly string? _externalDirectory;
    private readonly bool _encrypt;
    private readonly bool _externalCopyRequired;
    private readonly int _maxBackups;

    public BackupService(FishFarmContext context, IConfiguration configuration)
    {
        _context = context;
        _databasePath = RuntimePaths.ResolveDatabasePath(configuration);
        _backupDirectory = RuntimePaths.ResolveBackupDirectory(configuration);
        _encrypt = configuration.GetValue("Backup:Encrypt", true);
        _externalCopyRequired = configuration.GetValue("Backup:ExternalCopyRequired", false);
        _maxBackups = Math.Max(3, configuration.GetValue("Backup:MaxBackups", 10));
        var external = configuration["Backup:ExternalDirectory"];
        if (!string.IsNullOrWhiteSpace(external))
        {
            _externalDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(external));
        }

        Directory.CreateDirectory(_backupDirectory);
        _lastKnownBackupDirectory = _backupDirectory;
    }

    public Task<bool> CreateBackup(BackupType type = BackupType.Manual)
        => Task.FromResult(CreateBackupInternal(type));

    public Task<bool> RestoreBackup(string backupPath)
        => Task.FromResult(RestoreBackupInternal(backupPath));

    public Task<bool> ExportToCSV()
    {
        LoggingService.LogWarning("CSV export is not implemented and was not reported as successful.");
        return Task.FromResult(false);
    }

    public BackupInfo[] GetAvailableBackups() => GetBackups(_backupDirectory);

    private bool CreateBackupInternal(BackupType type)
    {
        var snapshot = Path.Combine(Path.GetTempPath(), $"aquafarm-snapshot-{Guid.NewGuid():N}.db");
        try
        {
            Directory.CreateDirectory(_backupDirectory);
            CreateOnlineSnapshot(snapshot);
            EnsureSqliteIntegrity(snapshot);

            var extension = _encrypt ? ".afbackup" : ".db";
            var filename = $"fishfarm_{type.ToString().ToLowerInvariant()}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}{extension}";
            var backupPath = Path.Combine(_backupDirectory, filename);
            var plainBytes = File.ReadAllBytes(snapshot);
            var storedBytes = _encrypt
                ? ProtectedData.Protect(plainBytes, BackupEntropy, DataProtectionScope.CurrentUser)
                : plainBytes;

            File.WriteAllBytes(backupPath, storedBytes);
            WriteChecksum(backupPath, storedBytes);
            VerifyChecksum(backupPath);

            if (!string.IsNullOrWhiteSpace(_externalDirectory))
            {
                CopyToExternalDirectory(backupPath);
            }
            else if (_externalCopyRequired)
            {
                throw new InvalidOperationException("An external backup copy is required but no external directory is configured.");
            }

            CleanOldBackups(_backupDirectory, _maxBackups);
            LoggingService.LogInfo("Encrypted SQLite backup created: {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.LogError(ex, "Backup creation failed for {DatabasePath}", _databasePath);
            return false;
        }
        finally
        {
            DeleteIfExists(snapshot);
        }
    }

    private bool RestoreBackupInternal(string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            return false;
        }

        var candidate = Path.Combine(Path.GetTempPath(), $"aquafarm-restore-{Guid.NewGuid():N}.db");
        var rollback = Path.Combine(Path.GetTempPath(), $"aquafarm-rollback-{Guid.NewGuid():N}.db");
        try
        {
            VerifyChecksum(backupPath);
            var storedBytes = File.ReadAllBytes(backupPath);
            var plainBytes = string.Equals(Path.GetExtension(backupPath), ".afbackup", StringComparison.OrdinalIgnoreCase)
                ? ProtectedData.Unprotect(storedBytes, BackupEntropy, DataProtectionScope.CurrentUser)
                : storedBytes;
            File.WriteAllBytes(candidate, plainBytes);
            EnsureSqliteIntegrity(candidate);

            if (File.Exists(_databasePath) && !CreateBackupInternal(BackupType.PreRestore))
            {
                throw new InvalidOperationException("The pre-restore safety backup failed.");
            }

            _context.Database.CloseConnection();
            if (File.Exists(_databasePath))
            {
                File.Copy(_databasePath, rollback, true);
            }

            File.Copy(candidate, _databasePath, true);
            EnsureSqliteIntegrity(_databasePath);
            LoggingService.LogInfo("Database restored and verified from {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            if (File.Exists(rollback))
            {
                File.Copy(rollback, _databasePath, true);
                LoggingService.LogWarning("Failed restore was rolled back to the previous database.");
            }
            LoggingService.LogError(ex, "Database restore failed from {BackupPath}", backupPath);
            return false;
        }
        finally
        {
            DeleteIfExists(candidate);
            DeleteIfExists(rollback);
        }
    }

    private void CreateOnlineSnapshot(string snapshotPath)
    {
        var source = (SqliteConnection)_context.Database.GetDbConnection();
        var shouldClose = source.State != ConnectionState.Open;
        if (shouldClose) source.Open();
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = snapshotPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };
            using var destination = new SqliteConnection(builder.ConnectionString);
            destination.Open();
            source.BackupDatabase(destination);
        }
        finally
        {
            if (shouldClose) source.Close();
        }
    }

    private static void EnsureSqliteIntegrity(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(command.ExecuteScalar());
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"SQLite integrity check failed: {result}");
        }
    }

    private void CopyToExternalDirectory(string backupPath)
    {
        Directory.CreateDirectory(_externalDirectory!);
        var destination = Path.Combine(_externalDirectory!, Path.GetFileName(backupPath));
        File.Copy(backupPath, destination, false);
        File.Copy(GetChecksumPath(backupPath), GetChecksumPath(destination), false);
    }

    private static void WriteChecksum(string backupPath, byte[] bytes)
        => File.WriteAllText(GetChecksumPath(backupPath), Convert.ToHexString(SHA256.HashData(bytes)));

    private static void VerifyChecksum(string backupPath)
    {
        var checksumPath = GetChecksumPath(backupPath);
        if (!File.Exists(checksumPath))
        {
            if (string.Equals(Path.GetExtension(backupPath), ".db", StringComparison.OrdinalIgnoreCase)) return;
            throw new InvalidDataException("The backup checksum file is missing.");
        }

        var expected = Convert.FromHexString(File.ReadAllText(checksumPath).Trim());
        var actual = SHA256.HashData(File.ReadAllBytes(backupPath));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new InvalidDataException("The backup checksum does not match.");
        }
    }

    private static string GetChecksumPath(string path) => path + ".sha256";

    public static BackupInfo[] GetBackups()
    {
        var directory = _lastKnownBackupDirectory ?? Path.Combine(RuntimePaths.GetApplicationDataDirectory(), "Backups");
        return GetBackups(directory);
    }

    private static BackupInfo[] GetBackups(string directory)
    {
        if (!Directory.Exists(directory)) return Array.Empty<BackupInfo>();
        return Directory.EnumerateFiles(directory)
            .Where(path => path.EndsWith(".afbackup", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file => new BackupInfo
            {
                FileName = file.Name,
                FullPath = file.FullName,
                CreatedDate = file.CreationTime,
                SizeMB = file.Length / (1024.0 * 1024.0),
                Type = DetectBackupType(file.Name),
                IsEncrypted = file.Extension.Equals(".afbackup", StringComparison.OrdinalIgnoreCase)
            }).ToArray();
    }

    public static BackupInfo? GetLatestBackup() => GetBackups().FirstOrDefault();
    public static int GetDaysSinceLastBackup()
    {
        var latest = GetLatestBackup();
        return latest == null ? int.MaxValue : Math.Max(0, (DateTime.Now - latest.CreatedDate).Days);
    }
    public static bool NeedsBackup(int daysSinceLastBackup = 7) => GetDaysSinceLastBackup() >= daysSinceLastBackup;

    private static BackupType DetectBackupType(string filename)
    {
        foreach (var type in Enum.GetValues<BackupType>())
        {
            if (filename.Contains($"_{type.ToString().ToLowerInvariant()}_", StringComparison.OrdinalIgnoreCase)) return type;
        }
        return BackupType.Manual;
    }

    private static void CleanOldBackups(string directory, int maxBackups)
    {
        foreach (var backup in GetBackups(directory).Skip(maxBackups))
        {
            DeleteIfExists(backup.FullPath);
            DeleteIfExists(GetChecksumPath(backup.FullPath));
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}

public enum BackupType { Manual, Scheduled, PreOperation, PreRestore }

public sealed class BackupInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public double SizeMB { get; set; }
    public BackupType Type { get; set; }
    public bool IsEncrypted { get; set; }
}
