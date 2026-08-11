using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace FishFarmManager.Services;

public sealed class UserSettingsDocument
{
    public ConnectionStringSettings ConnectionStrings { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public BackupSettings Backup { get; set; } = new();
    public UserInterfaceSettings UserInterface { get; set; } = new();
    public ReportSettings Reports { get; set; } = new();
}

public sealed class ConnectionStringSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public sealed class DatabaseSettings
{
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}

public sealed class BackupSettings
{
    public string Directory { get; set; } = string.Empty;
    public bool AutomaticEnabled { get; set; } = true;
    public string Frequency { get; set; } = "يومي";
    public int MaxBackups { get; set; } = 7;
}

public sealed class UserInterfaceSettings
{
    public string Language { get; set; } = SupportedCultures.Default;
    public string Theme { get; set; } = "الافتراضية";
    public int FontSize { get; set; } = 10;
    public bool ShowTooltips { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
}

public sealed class ReportSettings
{
    public string DateFormat { get; set; } = "yyyy/MM/dd";
    public string Currency { get; set; } = "ريال سعودي (SAR)";
    public string NumberFormat { get; set; } = "1,234.56";
    public bool AutoSave { get; set; } = true;
    public bool ShowCharts { get; set; } = true;
}

public sealed class UserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    private readonly IConfiguration _configuration;
    private readonly string _settingsPath;

    public UserSettingsService(IConfiguration configuration)
        : this(configuration, RuntimePaths.GetUserSettingsPath())
    {
    }

    public UserSettingsService(IConfiguration configuration, string settingsPath)
    {
        _configuration = configuration;
        _settingsPath = settingsPath;
    }

    public UserSettingsDocument Load()
    {
        if (File.Exists(_settingsPath))
        {
            var loaded = JsonSerializer.Deserialize<UserSettingsDocument>(
                File.ReadAllText(_settingsPath), JsonOptions);
            if (loaded != null)
            {
                Normalize(loaded);
                return loaded;
            }
        }

        var settings = new UserSettingsDocument
        {
            ConnectionStrings = new ConnectionStringSettings
            {
                DefaultConnection = RuntimePaths.ResolveConnectionString(_configuration)
            },
            Database = new DatabaseSettings
            {
                ConnectionTimeoutSeconds = Math.Clamp(
                    _configuration.GetValue("Database:ConnectionTimeoutSeconds", 30), 5, 300)
            },
            Backup = new BackupSettings
            {
                Directory = RuntimePaths.ResolveBackupDirectory(_configuration),
                MaxBackups = Math.Clamp(_configuration.GetValue("Backup:MaxBackups", 7), 1, 50)
            }
        };
        Normalize(settings);
        return settings;
    }

    public void Save(UserSettingsDocument settings)
    {
        Normalize(settings);
        ValidateDatabasePath(settings.ConnectionStrings.DefaultConnection);
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("تعذر تحديد مجلد الإعدادات.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, _settingsPath, true);
    }

    public static string ExtractDatabasePath(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource))
            throw new InvalidOperationException("يجب تحديد مسار قاعدة البيانات.");
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(builder.DataSource));
    }

    public static string CreateConnectionString(string databasePath, int timeoutSeconds)
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            DefaultTimeout = Math.Clamp(timeoutSeconds, 5, 300)
        }.ToString();
    }

    private static void Normalize(UserSettingsDocument settings)
    {
        settings.ConnectionStrings ??= new ConnectionStringSettings();
        settings.Database ??= new DatabaseSettings();
        settings.Backup ??= new BackupSettings();
        settings.UserInterface ??= new UserInterfaceSettings();
        settings.Reports ??= new ReportSettings();
        settings.Database.ConnectionTimeoutSeconds = Math.Clamp(
            settings.Database.ConnectionTimeoutSeconds, 5, 300);
        settings.Backup.MaxBackups = Math.Clamp(settings.Backup.MaxBackups, 1, 50);
        settings.UserInterface.FontSize = Math.Clamp(settings.UserInterface.FontSize, 8, 24);
        settings.UserInterface.Language = SupportedCultures.Normalize(settings.UserInterface.Language);
    }

    private static void ValidateDatabasePath(string connectionString)
    {
        var path = ExtractDatabasePath(connectionString);
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            throw new InvalidOperationException("مجلد قاعدة البيانات المحدد غير موجود.");
    }
}
