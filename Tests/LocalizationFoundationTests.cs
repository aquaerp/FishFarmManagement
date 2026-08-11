using System.Globalization;
using FishFarmManager.Services;
using FishFarmManager.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class LocalizationFoundationTests : IDisposable
{
    public LocalizationFoundationTests() =>
        LocalizationManager.SetCulture(SupportedCultures.Arabic);

    public void Dispose() =>
        LocalizationManager.SetCulture(SupportedCultures.Arabic);

    [Theory]
    [InlineData("العربية", SupportedCultures.Arabic)]
    [InlineData("Arabic", SupportedCultures.Arabic)]
    [InlineData("English", SupportedCultures.English)]
    [InlineData("الإنجليزية", SupportedCultures.English)]
    [InlineData("unsupported", SupportedCultures.Arabic)]
    public void Normalize_MigratesLegacyLanguageValues(string input, string expected)
    {
        Assert.Equal(expected, SupportedCultures.Normalize(input));
    }

    [Fact]
    public void Cultures_HaveExpectedDirectionAndGregorianBusinessCalendar()
    {
        Assert.True(SupportedCultures.IsRightToLeft(SupportedCultures.Arabic));
        Assert.False(SupportedCultures.IsRightToLeft(SupportedCultures.English));
        Assert.IsType<GregorianCalendar>(SupportedCultures.Create(SupportedCultures.Arabic).DateTimeFormat.Calendar);
    }

    [Fact]
    public void Resources_ResolveForBothSupportedCultures()
    {
        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        Assert.Equal("حفظ", LocalizationManager.Get("Save"));
        Assert.Equal("القيود اليومية", LocalizationManager.Get("JournalEntries"));

        LocalizationManager.SetCulture(SupportedCultures.English);
        Assert.Equal("Save", LocalizationManager.Get("Save"));
        Assert.Equal("Journal Entries", LocalizationManager.Get("JournalEntries"));
    }

    [Fact]
    public void Resources_HaveExactlyTheSameKeysInBothLanguages()
    {
        var arabicKeys = LocalizationManager.GetAvailableKeys(SupportedCultures.Arabic);
        var englishKeys = LocalizationManager.GetAvailableKeys(SupportedCultures.English);

        Assert.NotEmpty(arabicKeys);
        Assert.Equal(arabicKeys, englishKeys);
    }

    [Fact]
    public void MissingResource_ReturnsVisibleDiagnosticKey()
    {
        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        Assert.Equal("[Missing.Test.Key]", LocalizationManager.Get("Missing.Test.Key"));
    }

    [Theory]
    [InlineData(SupportedCultures.Arabic, RightToLeft.Yes, HorizontalAlignment.Right)]
    [InlineData(SupportedCultures.English, RightToLeft.No, HorizontalAlignment.Left)]
    public void CultureDirection_UpdatesFormAndInputAlignment(
        string cultureName,
        RightToLeft expectedDirection,
        HorizontalAlignment expectedAlignment)
    {
        LocalizationManager.SetCulture(cultureName);
        using var form = new Form();
        using var input = new TextBox();
        form.Controls.Add(input);

        ThemeManager.ApplyCultureDirection(form);

        Assert.Equal(expectedDirection, form.RightToLeft);
        Assert.Equal(expectedAlignment, input.TextAlign);
    }

    [Fact]
    public void BoundControlsAndGridColumns_UpdateWithoutRecreation()
    {
        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        using var form = LocalizationManager.Bind(new Form(), "SystemSettings");
        using var button = LocalizationManager.Bind(new Button(), "Save");
        using var grid = new DataGridView();
        var column = LocalizationManager.Bind(new DataGridViewTextBoxColumn(), "Account");
        grid.Columns.Add(column);
        form.Controls.Add(button);
        form.Controls.Add(grid);

        LocalizationManager.ApplyResources(form);
        Assert.Equal("إعدادات النظام", form.Text);
        Assert.Equal("حفظ", button.Text);
        Assert.Equal("الحساب", column.HeaderText);

        LocalizationManager.SetCulture(SupportedCultures.English);
        LocalizationManager.ApplyResources(form);
        Assert.Equal("System Settings", form.Text);
        Assert.Equal("Save", button.Text);
        Assert.Equal("Account", column.HeaderText);
    }

    [Fact]
    public void BusinessRuleErrors_AreLocalizedWithoutShowingDiagnosticText()
    {
        var exception = BusinessRuleError.Create(
            "JournalUnbalanced",
            "internal diagnostic text that must not reach the user");

        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        var arabic = LocalizationManager.GetUserMessage(exception);
        Assert.Contains("غير متوازن", arabic);
        Assert.DoesNotContain("internal diagnostic", arabic);

        LocalizationManager.SetCulture(SupportedCultures.English);
        var english = LocalizationManager.GetUserMessage(exception);
        Assert.Contains("not balanced", english);
        Assert.DoesNotContain("internal diagnostic", english);
    }

    [Fact]
    public void AutomaticBindings_LocalizeNestedMenuItems()
    {
        LocalizationManager.SetCulture(SupportedCultures.English);
        using var strip = new MenuStrip();
        var file = new ToolStripMenuItem("ملف");
        file.DropDownItems.Add("نسخ احتياطي");
        file.DropDownItems.Add("تصدير البيانات");
        var help = new ToolStripMenuItem("مساعدة");
        help.DropDownItems.Add("حول البرنامج");
        strip.Items.Add(file);
        strip.Items.Add(help);

        LocalizationManager.ApplyResources(strip);

        Assert.Equal("File", file.Text);
        Assert.Equal("Backup", file.DropDownItems[0].Text);
        Assert.Equal("Export Data", file.DropDownItems[1].Text);
        Assert.Equal("Help", help.Text);
        Assert.Equal("About", help.DropDownItems[0].Text);
    }

    [Fact]
    public void LoginForm_ChangesLanguageDirectionAndAccessibleLabelsLive()
    {
        var options = new DbContextOptionsBuilder<FishFarmContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new FishFarmContext(options);
        using var form = new LoginForm(new AuthenticationService(context));

        LocalizationManager.SetCulture(SupportedCultures.English);
        Assert.Equal("AquaFarm Pro - Sign In", form.Text);
        Assert.Equal(RightToLeft.No, form.RightToLeft);
        var englishButtons = Descendants(form).OfType<Button>().ToArray();
        Assert.Contains(englishButtons, button => button.Text == "Sign In");
        Assert.All(englishButtons, button => Assert.True(button.GetPreferredSize(Size.Empty).Width <= button.Width));
        Assert.Contains(Descendants(form).OfType<TextBox>(), input => input.AccessibleName == "Username:");
        Assert.Equal(AutoScaleMode.Dpi, form.AutoScaleMode);
        Assert.Equal(
            new[] { 0, 1, 2, 3, 4 },
            Descendants(form).Where(control => control.TabStop).Select(control => control.TabIndex).OrderBy(index => index).ToArray());

        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        Assert.Equal("AquaFarm Pro - تسجيل الدخول", form.Text);
        Assert.Equal(RightToLeft.Yes, form.RightToLeft);
        Assert.Contains(Descendants(form).OfType<Button>(), button => button.Text == "تسجيل الدخول");
        Assert.Contains(Descendants(form).OfType<TextBox>(), input => input.AccessibleName == "اسم المستخدم:");
    }

    [Fact]
    public void AccountingEnums_HaveResourcesInBothLanguages()
    {
        var enumTypes = new[]
        {
            typeof(AccountingAdjustmentType), typeof(ExchangeRatePurpose),
            typeof(ForeignMonetaryItemKind), typeof(FiscalPeriodStatus),
            typeof(JournalEntryStatus), typeof(AccountingConfigurationStatus),
            typeof(ExchangeRateStatus), typeof(ForeignMonetaryItemStatus),
            typeof(VATReturnStatus), typeof(StockMovementType)
        };

        foreach (var culture in SupportedCultures.All)
        {
            LocalizationManager.SetCulture(culture);
            foreach (var enumType in enumTypes)
            foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
            {
                var key = $"Enum.{enumType.Name}.{value}";
                Assert.NotEqual($"[{key}]", LocalizationManager.Get(key));
            }
        }
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
