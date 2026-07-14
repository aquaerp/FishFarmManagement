using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace FishFarmManager.Services;

public static class RuntimePaths
{
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = $"Data Source={Path.Combine(GetApplicationDataDirectory(), "FishFarm.db")}";
        }

        var expanded = Environment.ExpandEnvironmentVariables(configured);
        var builder = new SqliteConnectionStringBuilder(expanded);
        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException("DefaultConnection must define a SQLite Data Source.");
        }

        var dataSource = builder.DataSource;
        if (!Path.IsPathRooted(dataSource))
        {
            dataSource = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataSource));
        }

        builder.DataSource = Path.GetFullPath(dataSource);
        return builder.ConnectionString;
    }

    public static string ResolveDatabasePath(IConfiguration configuration)
    {
        return Path.GetFullPath(new SqliteConnectionStringBuilder(ResolveConnectionString(configuration)).DataSource);
    }

    public static string ResolveBackupDirectory(IConfiguration configuration)
    {
        var configured = configuration["Backup:Directory"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(GetApplicationDataDirectory(), "Backups")
            : Environment.ExpandEnvironmentVariables(configured);

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(GetApplicationDataDirectory(), path);
        }

        return Path.GetFullPath(path);
    }

    public static string GetApplicationDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FishFarmManager");
    }
}

