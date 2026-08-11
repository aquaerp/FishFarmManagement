using System.Globalization;

namespace FishFarmManager.Services;

public static class CultureFormatter
{
    public const string BusinessDatePattern = "dd/MM/yyyy";
    public const string BusinessDateTimePattern = "dd/MM/yyyy HH:mm";

    public static string FormatDate(DateTime value, CultureInfo? culture = null) =>
        value.ToString(BusinessDatePattern, culture ?? LocalizationManager.CurrentCulture);

    public static string FormatDateTime(DateTime value, CultureInfo? culture = null) =>
        value.ToString(BusinessDateTimePattern, culture ?? LocalizationManager.CurrentCulture);

    public static string FormatNumber(decimal value, int decimalPlaces = 2, CultureInfo? culture = null)
    {
        if (decimalPlaces is < 0 or > 6)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces));

        return value.ToString($"N{decimalPlaces}", culture ?? LocalizationManager.CurrentCulture);
    }

    public static string FormatSar(decimal amount, CultureInfo? culture = null)
    {
        var selectedCulture = culture ?? LocalizationManager.CurrentCulture;
        var formattedAmount = FormatNumber(amount, 2, selectedCulture);
        return selectedCulture.TextInfo.IsRightToLeft
            ? $"{formattedAmount} {LocalizationManager.Get("CurrencySar")}"
            : $"{LocalizationManager.Get("CurrencySar")} {formattedAmount}";
    }

    public static bool TryParseDecimal(string? value, out decimal result, CultureInfo? culture = null) =>
        decimal.TryParse(
            value,
            NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
            culture ?? LocalizationManager.CurrentCulture,
            out result);
}
