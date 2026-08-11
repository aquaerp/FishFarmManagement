using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class CultureFormatterTests : IDisposable
{
    public CultureFormatterTests() =>
        LocalizationManager.SetCulture(SupportedCultures.Arabic);

    public void Dispose() =>
        LocalizationManager.SetCulture(SupportedCultures.Arabic);

    [Theory]
    [InlineData(SupportedCultures.Arabic, "31/12/2026")]
    [InlineData(SupportedCultures.English, "31/12/2026")]
    public void BusinessDate_RemainsGregorianAndUnambiguous(string culture, string expected)
    {
        LocalizationManager.SetCulture(culture);
        Assert.Equal(expected, CultureFormatter.FormatDate(new DateTime(2026, 12, 31)));
    }

    [Fact]
    public void SarSymbol_FollowsActiveLanguage()
    {
        LocalizationManager.SetCulture(SupportedCultures.Arabic);
        Assert.EndsWith(" ر.س", CultureFormatter.FormatSar(1234.5m));

        LocalizationManager.SetCulture(SupportedCultures.English);
        Assert.StartsWith("SAR ", CultureFormatter.FormatSar(1234.5m));
    }

    [Theory]
    [InlineData(SupportedCultures.Arabic)]
    [InlineData(SupportedCultures.English)]
    public void Decimal_RoundTripsUsingActiveCulture(string culture)
    {
        LocalizationManager.SetCulture(culture);
        var text = CultureFormatter.FormatNumber(1234.56m);

        Assert.True(CultureFormatter.TryParseDecimal(text, out var parsed));
        Assert.Equal(1234.56m, parsed);
    }
}
