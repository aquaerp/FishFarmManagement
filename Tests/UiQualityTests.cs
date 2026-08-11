using FishFarmManager.Forms;
using FishFarmManager.Services;
using Microsoft.Data.Sqlite;

namespace FishFarmManager.Tests;

public sealed class UiQualityTests
{
    [Theory]
    [InlineData("نص عربي طويل داخل الزر")]
    [InlineData("إضافة قيد جديد")]
    public void ThemeButtons_GrowWithTextAndExposeAccessibleName(string text)
    {
        using var button = ThemeManager.CreatePrimaryButton(text);
        Assert.True(button.AutoSize);
        Assert.True(button.MinimumSize.Width >= 120);
        Assert.True(button.MinimumSize.Height >= 40);
        Assert.Equal(text, button.AccessibleName);
    }

    [Theory]
    [InlineData(9.5f)]
    [InlineData(11.875f)]
    [InlineData(14.25f)]
    [InlineData(19f)]
    public void ArabicButton_PreferredSizeGrowsAcrossDpiEquivalentFontScales(float fontSize)
    {
        using var button = ThemeManager.CreateSuccessButton("إنشاء نسخة احتياطية الآن");
        button.Font = new Font(button.Font.FontFamily, fontSize, FontStyle.Bold);
        var preferred = button.GetPreferredSize(Size.Empty);
        Assert.True(preferred.Width >= TextRenderer.MeasureText(button.Text, button.Font).Width);
        Assert.True(preferred.Height >= TextRenderer.MeasureText(button.Text, button.Font).Height);
    }

    [Fact]
    public void UserConnectionString_PersistsTimeoutAndAbsolutePath()
    {
        var path = Path.Combine(Path.GetTempPath(), "fishfarm-ui-quality.db");
        var connectionString = UserSettingsService.CreateConnectionString(path, 75);
        var builder = new SqliteConnectionStringBuilder(connectionString);
        Assert.Equal(Path.GetFullPath(path), builder.DataSource);
        Assert.Equal(75, builder.DefaultTimeout);
    }

    [Fact]
    public void UserSettings_RoundTripAllVisibleOptions()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fishfarm-settings-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(root, "user-settings.json");
        Directory.CreateDirectory(root);
        try
        {
            var databasePath = Path.Combine(root, "FishFarm.db");
            var configuration = RuntimePathsTests.BuildConfiguration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}",
                ["Backup:Directory"] = Path.Combine(root, "Backups")
            });
            var service = new UserSettingsService(configuration, settingsPath);
            var settings = service.Load();
            settings.Database.ConnectionTimeoutSeconds = 90;
            settings.Backup.Frequency = "أسبوعي";
            settings.UserInterface.FontSize = 14;
            settings.Reports.DateFormat = "dd/MM/yyyy";
            service.Save(settings);

            var reloaded = service.Load();
            Assert.Equal(90, reloaded.Database.ConnectionTimeoutSeconds);
            Assert.Equal("أسبوعي", reloaded.Backup.Frequency);
            Assert.Equal(14, reloaded.UserInterface.FontSize);
            Assert.Equal("dd/MM/yyyy", reloaded.Reports.DateFormat);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void JournalDraftDialog_ContainsTheAddWorkflowControls()
    {
        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        using var dialog = new AccountingJournalDraftDialog(
            new[] { (1, "2026-01") }, new[] { (1, "1000 — النقدية") });
        var controls = Descendants(dialog).ToList();
        Assert.Contains(controls.OfType<Button>(), button => button.Text == "حفظ القيد كمسودة");
        Assert.Contains(controls.OfType<DataGridView>(), grid => grid.AccessibleName == "أسطر القيد المحاسبي");
        Assert.Equal(AutoScaleMode.Dpi, dialog.AutoScaleMode);
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
